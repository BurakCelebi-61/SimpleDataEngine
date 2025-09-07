using SimpleDataEngine.Audit;
using SimpleDataEngine.Performance;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Manages data segments for entities
    /// </summary>
    public class SegmentManager : IDisposable
    {
        private readonly string _entityName;
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly string _segmentDirectory;
        private readonly Dictionary<string, Segment> _activeSegments;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public SegmentManager(string entityName, HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));

            _segmentDirectory = Path.Combine(_config.DatabasePath, "segments", _entityName);
            _activeSegments = new Dictionary<string, Segment>();
        }

        /// <summary>
        /// Initialize the segment manager
        /// </summary>
        public async Task InitializeAsync()
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("SegmentManagerInit", "Storage");

            try
            {
                await _fileHandler.CreateDirectoryAsync(_segmentDirectory);

                // Load existing segments
                var segmentFiles = await _fileHandler.GetFilesAsync(_segmentDirectory, "*.seg");

                foreach (var segmentFile in segmentFiles)
                {
                    var segmentName = Path.GetFileNameWithoutExtension(segmentFile);
                    var segment = new Segment(segmentName, segmentFile, _config, _fileHandler);
                    await segment.LoadAsync();

                    lock (_lock)
                    {
                        _activeSegments[segmentName] = segment;
                    }
                }

                operation.MarkSuccess();

                AuditLogger.Log("SEGMENT_MANAGER_INITIALIZED", new
                {
                    EntityName = _entityName,
                    SegmentCount = _activeSegments.Count
                }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("SEGMENT_MANAGER_INIT_FAILED", ex, new { EntityName = _entityName });
                throw;
            }
        }

        /// <summary>
        /// Store data in a segment
        /// </summary>
        public async Task<string> StoreAsync(string data)
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("SegmentStore", "Storage");

            try
            {
                var id = Guid.NewGuid().ToString();
                var segment = await GetOrCreateActiveSegmentAsync();

                await segment.StoreAsync(id, data);

                operation.MarkSuccess();

                AuditLogger.Log("SEGMENT_DATA_STORED", new
                {
                    EntityName = _entityName,
                    SegmentName = segment.Name,
                    DataId = id,
                    DataSize = data.Length
                }, AuditCategory.Database);

                return id;
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("SEGMENT_STORE_FAILED", ex, new { EntityName = _entityName });
                throw;
            }
        }

        /// <summary>
        /// Retrieve data from segments
        /// </summary>
        public async Task<string?> RetrieveAsync(string id)
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("SegmentRetrieve", "Storage");

            try
            {
                lock (_lock)
                {
                    foreach (var segment in _activeSegments.Values)
                    {
                        var data = segment.RetrieveAsync(id).Result;
                        if (data != null)
                        {
                            operation.MarkSuccess();

                            AuditLogger.Log("SEGMENT_DATA_RETRIEVED", new
                            {
                                EntityName = _entityName,
                                SegmentName = segment.Name,
                                DataId = id
                            }, AuditCategory.Database);

                            return data;
                        }
                    }
                }

                operation.MarkSuccess();
                return null;
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("SEGMENT_RETRIEVE_FAILED", ex, new { EntityName = _entityName, DataId = id });
                throw;
            }
        }

        /// <summary>
        /// Flush all segments
        /// </summary>
        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("SegmentFlush", "Storage");

            try
            {
                var flushTasks = new List<Task>();

                lock (_lock)
                {
                    foreach (var segment in _activeSegments.Values)
                    {
                        flushTasks.Add(segment.FlushAsync());
                    }
                }

                if (flushTasks.Any())
                {
                    await Task.WhenAll(flushTasks);
                }

                operation.MarkSuccess();

                AuditLogger.Log("SEGMENTS_FLUSHED", new
                {
                    EntityName = _entityName,
                    SegmentCount = _activeSegments.Count
                }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("SEGMENT_FLUSH_FAILED", ex, new { EntityName = _entityName });
                throw;
            }
        }

        /// <summary>
        /// Get or create an active segment for writing
        /// </summary>
        private async Task<Segment> GetOrCreateActiveSegmentAsync()
        {
            lock (_lock)
            {
                // Find a segment that has space
                foreach (var segment in _activeSegments.Values)
                {
                    if (segment.HasSpace)
                    {
                        return segment;
                    }
                }

                // Create new segment
                var segmentName = $"seg_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
                var segmentPath = Path.Combine(_segmentDirectory, $"{segmentName}.seg");
                var newSegment = new Segment(segmentName, segmentPath, _config, _fileHandler);

                newSegment.InitializeAsync().Wait();
                _activeSegments[segmentName] = newSegment;

                return newSegment;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                FlushAsync().Wait(TimeSpan.FromSeconds(30));

                lock (_lock)
                {
                    foreach (var segment in _activeSegments.Values)
                    {
                        segment.Dispose();
                    }
                    _activeSegments.Clear();
                }

                _disposed = true;

                AuditLogger.Log("SEGMENT_MANAGER_DISPOSED", new { EntityName = _entityName }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("SEGMENT_MANAGER_DISPOSE_FAILED", ex, new { EntityName = _entityName });
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SegmentManager));
            }
        }
    }
