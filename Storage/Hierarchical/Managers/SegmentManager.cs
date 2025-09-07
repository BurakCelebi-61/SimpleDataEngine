using SimpleDataEngine.Audit;
using SimpleDataEngine.Storage.Hierarchical.Models;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static SimpleDataEngine.Storage.Hierarchical.Models.SegmentData<T>;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Manages segment operations for a specific entity
    /// </summary>
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

        #region Initialization

        /// <summary>
        /// Initialize segment manager
        /// </summary>
        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_entityPath))
            {
                Directory.CreateDirectory(_entityPath);
            }

            await _entityMetadataManager.InitializeAsync();
            await LoadSegmentCacheAsync();
        }

        private async Task LoadSegmentCacheAsync()
        {
            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            if (entityMetadata?.Segments != null)
            {
                foreach (var segment in entityMetadata.Segments)
                {
                    _segmentCache.TryAdd(segment.SegmentId, segment);
                }
            }
        }

        #endregion

        #region Segment Creation & Management

        /// <summary>
        /// Get current active segment for writing, create new if needed
        /// </summary>
        public async Task<int> GetActiveSegmentIdAsync()
        {
            await _segmentLock.WaitAsync();
            try
            {
                var entityMetadata = await _entityMetadataManager.GetMetadataAsync();

                // Find active segment
                var activeSegment = entityMetadata?.Segments?
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.SegmentId)
                    .FirstOrDefault();

                // Check if we need a new segment
                if (activeSegment == null || await ShouldCreateNewSegmentAsync(activeSegment))
                {
                    return await CreateNewSegmentAsync();
                }

                return activeSegment.SegmentId;
            }
            finally
            {
                _segmentLock.Release();
            }
        }

        /// <summary>
        /// Create a new segment
        /// </summary>
        public async Task<int> CreateNewSegmentAsync()
        {
            await _segmentLock.WaitAsync();
            try
            {
                var entityMetadata = await _entityMetadataManager.GetMetadataAsync();

                // Deactivate current segments
                if (entityMetadata?.Segments != null)
                {
                    foreach (var segment in entityMetadata.Segments.Where(s => s.IsActive))
                    {
                        segment.IsActive = false;
                        segment.LastModified = DateTime.UtcNow;
                    }
                }

                // Create new segment
                var newSegmentId = (entityMetadata?.Segments?.Max(s => s.SegmentId) ?? 0) + 1;
                var newSegment = new SegmentMetadata
                {
                    SegmentId = newSegmentId,
                    FileName = GenerateSegmentFileName(newSegmentId),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    IsActive = true,
                    RecordCount = 0,
                    FileSizeBytes = 0,
                    Checksum = string.Empty
                };

                // Add to metadata
                entityMetadata ??= new EntityMetadata { EntityName = _entityName, Segments = new List<SegmentMetadata>() };
                entityMetadata.Segments.Add(newSegment);

                // Save metadata
                await _entityMetadataManager.SaveMetadataAsync(entityMetadata);

                // Add to cache
                _segmentCache.TryAdd(newSegmentId, newSegment);

                await AuditLogger.LogAsync($"New segment created for entity {_entityName}",
                    new { SegmentId = newSegmentId, FileName = newSegment.FileName });

                return newSegmentId;
            }
            finally
            {
                _segmentLock.Release();
            }
        }

        /// <summary>
        /// Check if a new segment should be created
        /// </summary>
        private async Task<bool> ShouldCreateNewSegmentAsync(SegmentMetadata segment)
        {
            if (!segment.IsActive) return true;

            // Check file size limit
            var filePath = GetSegmentFilePath(segment.SegmentId);
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                var sizeLimitBytes = _config.MaxSegmentSizeMB * 1024 * 1024;

                if (fileInfo.Length >= sizeLimitBytes)
                {
                    return true;
                }
            }

            // Check record count limit
            if (segment.RecordCount >= _config.MaxRecordsPerSegment)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Segment I/O Operations

        /// <summary>
        /// Write data to a specific segment
        /// </summary>
        public async Task WriteToSegmentAsync<T>(int segmentId, IEnumerable<T> data) where T : class
        {
            if (!data.Any()) return;

            var filePath = GetSegmentFilePath(segmentId);
            var segmentData = new SegmentData<T>
            {
                SegmentId = segmentId,
                CreatedAt = DateTime.UtcNow,
                Records = data.ToList()
            };

            try
            {
                await _fileHandler.WriteAsync(filePath, segmentData);

                // Update segment metadata
                await UpdateSegmentMetadataAfterWriteAsync(segmentId, segmentData.Records.Count, filePath);

                await AuditLogger.LogAsync($"Data written to segment {segmentId} in entity {_entityName}",
                    new { RecordCount = segmentData.Records.Count, FilePath = filePath });
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to write to segment {segmentId} in entity {_entityName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Read data from a specific segment
        /// </summary>
        public async Task<SegmentData<T>> ReadFromSegmentAsync<T>(int segmentId) where T : class
        {
            var filePath = GetSegmentFilePath(segmentId);

            if (!File.Exists(filePath))
            {
                return new SegmentData<T> { SegmentId = segmentId, Records = new List<T>() };
            }

            try
            {
                var segmentData = await _fileHandler.ReadAsync<SegmentData<T>>(filePath);

                await AuditLogger.LogAsync($"Data read from segment {segmentId} in entity {_entityName}",
                    new { RecordCount = segmentData?.Records?.Count ?? 0, FilePath = filePath });

                return segmentData ?? new SegmentData<T> { SegmentId = segmentId, Records = new List<T>() };
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to read from segment {segmentId} in entity {_entityName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Append data to an existing segment
        /// </summary>
        public async Task AppendToSegmentAsync<T>(int segmentId, IEnumerable<T> newData) where T : class
        {
            if (!newData.Any()) return;

            var existingData = await ReadFromSegmentAsync<T>(segmentId);
            existingData.Records.AddRange(newData);
            existingData.LastModified = DateTime.UtcNow;

            await WriteToSegmentAsync(segmentId, existingData.Records);
        }

        #endregion

        #region Segment Queries

        /// <summary>
        /// Get all segment IDs for the entity
        /// </summary>
        public async Task<List<int>> GetAllSegmentIdsAsync()
        {
            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            return entityMetadata?.Segments?.Select(s => s.SegmentId).OrderBy(id => id).ToList() ?? new List<int>();
        }

        /// <summary>
        /// Get segment metadata by ID
        /// </summary>
        public async Task<SegmentMetadata> GetSegmentMetadataAsync(int segmentId)
        {
            if (_segmentCache.TryGetValue(segmentId, out var cached))
            {
                return cached;
            }

            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            var segment = entityMetadata?.Segments?.FirstOrDefault(s => s.SegmentId == segmentId);

            if (segment != null)
            {
                _segmentCache.TryAdd(segmentId, segment);
            }

            return segment;
        }

        /// <summary>
        /// Get segments within a date range
        /// </summary>
        public async Task<List<SegmentMetadata>> GetSegmentsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            return entityMetadata?.Segments?
                .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
                .OrderBy(s => s.CreatedAt)
                .ToList() ?? new List<SegmentMetadata>();
        }

        /// <summary>
        /// Get active segments
        /// </summary>
        public async Task<List<SegmentMetadata>> GetActiveSegmentsAsync()
        {
            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            return entityMetadata?.Segments?
                .Where(s => s.IsActive)
                .OrderBy(s => s.SegmentId)
                .ToList() ?? new List<SegmentMetadata>();
        }

        #endregion

        #region Maintenance Operations

        /// <summary>
        /// Cleanup old segments based on cutoff date
        /// </summary>
        public async Task CleanupOldSegmentsAsync(DateTime cutoffDate)
        {
            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            if (entityMetadata?.Segments == null) return;

            var segmentsToRemove = entityMetadata.Segments
                .Where(s => !s.IsActive && s.LastModified < cutoffDate)
                .ToList();

            foreach (var segment in segmentsToRemove)
            {
                var filePath = GetSegmentFilePath(segment.SegmentId);

                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    entityMetadata.Segments.Remove(segment);
                    _segmentCache.TryRemove(segment.SegmentId, out _);

                    await AuditLogger.LogAsync($"Old segment cleaned up: {segment.SegmentId} in entity {_entityName}");
                }
                catch (Exception ex)
                {
                    await AuditLogger.LogErrorAsync($"Failed to cleanup segment {segment.SegmentId} in entity {_entityName}", ex);
                }
            }

            if (segmentsToRemove.Any())
            {
                await _entityMetadataManager.SaveMetadataAsync(entityMetadata);
            }
        }

        /// <summary>
        /// Verify segment integrity
        /// </summary>
        public async Task<bool> VerifySegmentIntegrityAsync(int segmentId)
        {
            var segment = await GetSegmentMetadataAsync(segmentId);
            if (segment == null) return false;

            var filePath = GetSegmentFilePath(segmentId);
            if (!File.Exists(filePath)) return false;

            try
            {
                // Calculate current checksum
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var currentChecksum = CalculateChecksum(fileBytes);

                return string.Equals(segment.Checksum, currentChecksum, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compact segments by merging small ones
        /// </summary>
        public async Task CompactSegmentsAsync()
        {
            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            if (entityMetadata?.Segments == null) return;

            var inactiveSegments = entityMetadata.Segments
                .Where(s => !s.IsActive && s.RecordCount < _config.MaxRecordsPerSegment / 4)
                .OrderBy(s => s.SegmentId)
                .ToList();

            // Group small segments for merging
            var segmentGroups = new List<List<SegmentMetadata>>();
            var currentGroup = new List<SegmentMetadata>();
            long currentGroupSize = 0;

            foreach (var segment in inactiveSegments)
            {
                if (currentGroupSize + segment.FileSizeBytes > _config.MaxSegmentSizeMB * 1024 * 1024 / 2)
                {
                    if (currentGroup.Any())
                    {
                        segmentGroups.Add(currentGroup);
                        currentGroup = new List<SegmentMetadata>();
                        currentGroupSize = 0;
                    }
                }

                currentGroup.Add(segment);
                currentGroupSize += segment.FileSizeBytes;
            }

            if (currentGroup.Count > 1)
            {
                segmentGroups.Add(currentGroup);
            }

            // Merge segment groups
            foreach (var group in segmentGroups.Where(g => g.Count > 1))
            {
                await MergeSegmentsAsync(group);
            }
        }

        private async Task MergeSegmentsAsync(List<SegmentMetadata> segments)
        {
            // Implementation for merging segments would go here
            // This is a complex operation that would need to:
            // 1. Read all data from source segments
            // 2. Combine into a new segment
            // 3. Update metadata
            // 4. Remove old segments
            await AuditLogger.LogAsync($"Segment merge operation started for entity {_entityName}",
                new { SegmentIds = segments.Select(s => s.SegmentId).ToArray() });
        }

        #endregion

        #region Utilities

        private async Task UpdateSegmentMetadataAfterWriteAsync(int segmentId, int recordCount, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var checksum = CalculateChecksum(fileBytes);

            var entityMetadata = await _entityMetadataManager.GetMetadataAsync();
            var segment = entityMetadata?.Segments?.FirstOrDefault(s => s.SegmentId == segmentId);

            if (segment != null)
            {
                segment.RecordCount = recordCount;
                segment.FileSizeBytes = fileInfo.Length;
                segment.LastModified = DateTime.UtcNow;
                segment.Checksum = checksum;

                // Update cache
                _segmentCache.AddOrUpdate(segmentId, segment, (key, old) => segment);

                // Save metadata
                await _entityMetadataManager.SaveMetadataAsync(entityMetadata);
            }
        }

        private string GenerateSegmentFileName(int segmentId)
        {
            return $"{_entityName}_segment_{segmentId:D6}{_config.FileExtension}";
        }

        private string GetSegmentFilePath(int segmentId)
        {
            var fileName = GenerateSegmentFileName(segmentId);
            return Path.Combine(_entityPath, fileName);
        }

        private string CalculateChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _segmentLock?.Dispose();
                _entityMetadataManager?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}