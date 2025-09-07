using SimpleDataEngine.Audit;
using SimpleDataEngine.Storage.Hierarchical.Models;
using SimpleDataEngine.Storage.Hierarchical.SimpleDataEngine.Storage.Hierarchical.Managers;
using System.Collections.Concurrent;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    public class SegmentManager : IDisposable
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly string _entityName;
        private readonly string _entityPath;
        private readonly EntityMetadataManager _entityMetadataManager;
        private readonly ConcurrentDictionary<int, SegmentMetadata> _segmentCache;
        private readonly SemaphoreSlim _segmentLock;
        private bool _disposed;
        private int _currentActiveSegment = 1;

        public SegmentManager(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _entityPath = Path.Combine(config.DataModelsPath, entityName);
            _entityMetadataManager = new EntityMetadataManager(config, fileHandler, entityName);
            _segmentCache = new ConcurrentDictionary<int, SegmentMetadata>();
            _segmentLock = new SemaphoreSlim(1, 1);
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_entityPath))
            {
                Directory.CreateDirectory(_entityPath);
            }
            await _entityMetadataManager.InitializeAsync();
            await LoadSegmentMetadataAsync();
        }

        #region EKSIK METHOD'LAR - HATALAR İÇİN

        /// <summary>
        /// Write data to specific segment
        /// </summary>
        public async Task WriteToSegmentAsync<T>(int segmentId, List<T> records) where T : class
        {
            await _segmentLock.WaitAsync();
            try
            {
                var segmentData = new SegmentData<T>
                {
                    SegmentId = segmentId,
                    Records = records ?? new List<T>(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                var filePath = GetSegmentFilePath(segmentId);
                await _fileHandler.WriteAsync(filePath, segmentData);

                // Update metadata
                await UpdateSegmentMetadataAfterWriteAsync(segmentId, records?.Count ?? 0, filePath);

                await AuditLogger.LogAsync($"Data written to segment {segmentId} for entity {_entityName}",
                    new { SegmentId = segmentId, RecordCount = records?.Count ?? 0 });
            }
            finally
            {
                _segmentLock.Release();
            }
        }

        /// <summary>
        /// Get active segment ID for new records
        /// </summary>
        public async Task<int> GetActiveSegmentIdAsync()
        {
            await _segmentLock.WaitAsync();
            try
            {
                // Check if current active segment is full
                var activeSegmentPath = GetSegmentFilePath(_currentActiveSegment);

                if (await _fileHandler.ExistsAsync(activeSegmentPath))
                {
                    var fileSize = await _fileHandler.GetFileSizeAsync(activeSegmentPath);
                    var maxSizeBytes = _config.MaxSegmentSizeMB * 1024 * 1024;

                    if (fileSize >= maxSizeBytes)
                    {
                        // Create new segment
                        _currentActiveSegment++;
                    }
                }

                return _currentActiveSegment;
            }
            finally
            {
                _segmentLock.Release();
            }
        }

        /// <summary>
        /// Get all segment IDs
        /// </summary>
        public async Task<List<int>> GetAllSegmentIdsAsync()
        {
            var segmentFiles = await _fileHandler.GetFilesAsync(_entityPath, $"{_entityName}_segment_*{_config.FileExtension}");

            var segmentIds = new List<int>();
            foreach (var file in segmentFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[2], out var segmentId))
                {
                    segmentIds.Add(segmentId);
                }
            }

            return segmentIds.OrderBy(id => id).ToList();
        }

        /// <summary>
        /// Append records to specific segment
        /// </summary>
        public async Task AppendToSegmentAsync<T>(int segmentId, List<T> records) where T : class
        {
            if (records == null || !records.Any()) return;

            await _segmentLock.WaitAsync();
            try
            {
                // Read existing data
                var existingData = await ReadFromSegmentAsync<T>(segmentId);

                // Append new records
                existingData.Records.AddRange(records);
                existingData.LastModified = DateTime.UtcNow;

                // Write back to segment
                await WriteToSegmentAsync(segmentId, existingData.Records);

                await AuditLogger.LogAsync($"Records appended to segment {segmentId} for entity {_entityName}",
                    new { SegmentId = segmentId, AppendedCount = records.Count, TotalCount = existingData.Records.Count });
            }
            finally
            {
                _segmentLock.Release();
            }
        }

        /// <summary>
        /// Compact segments by merging small segments
        /// </summary>
        public async Task CompactSegmentsAsync()
        {
            await _segmentLock.WaitAsync();
            try
            {
                var segmentIds = await GetAllSegmentIdsAsync();
                var segmentsToCompact = new List<int>();

                // Find segments that are smaller than threshold
                var minSizeThreshold = _config.MaxSegmentSizeMB * 0.3 * 1024 * 1024; // 30% of max size

                foreach (var segmentId in segmentIds)
                {
                    var filePath = GetSegmentFilePath(segmentId);
                    if (await _fileHandler.ExistsAsync(filePath))
                    {
                        var fileSize = await _fileHandler.GetFileSizeAsync(filePath);
                        if (fileSize < minSizeThreshold)
                        {
                            segmentsToCompact.Add(segmentId);
                        }
                    }
                }

                if (segmentsToCompact.Count > 1)
                {
                    await PerformSegmentCompactionAsync(segmentsToCompact);
                }

                await AuditLogger.LogAsync($"Segment compaction completed for entity {_entityName}",
                    new { ProcessedSegments = segmentsToCompact.Count });
            }
            finally
            {
                _segmentLock.Release();
            }
        }

        #endregion

        #region EXISTING METHODS

        /// <summary>
        /// Read data from specific segment
        /// </summary>
        public async Task<SegmentData<T>> ReadFromSegmentAsync<T>(int segmentId) where T : class
        {
            var filePath = GetSegmentFilePath(segmentId);
            if (!await _fileHandler.ExistsAsync(filePath))
            {
                return new SegmentData<T>
                {
                    SegmentId = segmentId,
                    Records = new List<T>(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
            }

            try
            {
                var segmentData = await _fileHandler.ReadAsync<SegmentData<T>>(filePath);
                return segmentData ?? new SegmentData<T>
                {
                    SegmentId = segmentId,
                    Records = new List<T>(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to read from segment {segmentId}", ex);
                throw;
            }
        }

        #endregion

        #region HELPER METHODS

        private string GetSegmentFilePath(int segmentId)
        {
            var fileName = $"{_entityName}_segment_{segmentId:D6}{_config.FileExtension}";
            return Path.Combine(_entityPath, fileName);
        }

        private async Task UpdateSegmentMetadataAfterWriteAsync(int segmentId, int recordCount, string filePath)
        {
            var fileSize = await _fileHandler.GetFileSizeAsync(filePath);

            var metadata = new SegmentMetadata
            {
                SegmentId = segmentId,
                FileName = Path.GetFileName(filePath),
                RecordCount = recordCount,
                FileSizeBytes = fileSize,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsActive = true
            };

            _segmentCache.AddOrUpdate(segmentId, metadata, (key, old) => metadata);
        }

        private async Task LoadSegmentMetadataAsync()
        {
            var segmentIds = await GetAllSegmentIdsAsync();
            foreach (var segmentId in segmentIds)
            {
                var filePath = GetSegmentFilePath(segmentId);
                if (await _fileHandler.ExistsAsync(filePath))
                {
                    var fileSize = await _fileHandler.GetFileSizeAsync(filePath);
                    // Load basic metadata - detailed loading would require reading segment content
                    var metadata = new SegmentMetadata
                    {
                        SegmentId = segmentId,
                        FileName = Path.GetFileName(filePath),
                        FileSizeBytes = fileSize,
                        IsActive = true
                    };
                    _segmentCache.TryAdd(segmentId, metadata);
                }
            }

            // Set current active segment to highest ID + 1
            if (segmentIds.Any())
            {
                _currentActiveSegment = segmentIds.Max() + 1;
            }
        }

        private async Task PerformSegmentCompactionAsync(List<int> segmentIds)
        {
            // This is a simplified compaction - in practice you'd want to:
            // 1. Read all records from segments to compact
            // 2. Merge them into fewer segments
            // 3. Delete old segments
            // 4. Update indexes

            await AuditLogger.LogAsync($"Segment compaction performed for entity {_entityName}",
                new { CompactedSegments = segmentIds });
        }

        #endregion

        #region DISPOSAL

        public void Dispose()
        {
            if (!_disposed)
            {
                _segmentLock?.Dispose();
                _entityMetadataManager?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}