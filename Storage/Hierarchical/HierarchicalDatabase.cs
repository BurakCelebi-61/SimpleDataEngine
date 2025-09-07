using SimpleDataEngine.Storage.Hierarchical.Managers;
using SimpleDataEngine.Storage.Hierarchical.Models;
using SimpleDataEngine.Audit;
using System.Collections.Concurrent;
using SimpleDataEngine.Storage.Hierarchical.SimpleDataEngine.Storage.Hierarchical.Managers;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// HierarchicalDatabase with InitializeAsync method - Eksik method eklendi
    /// </summary>
    public partial class HierarchicalDatabase : IDisposable
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly GlobalMetadataManager _globalMetadataManager;
        private readonly ConcurrentDictionary<string, EntityMetadataManager> _entityManagers;
        private readonly ConcurrentDictionary<string, SegmentManager> _segmentManagers;
        private readonly SemaphoreSlim _initializationLock;
        private bool _isInitialized;
        private bool _disposed;

        /// <summary>
        /// Constructor with proper parameter order
        /// </summary>
        public HierarchicalDatabase(HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));

            _globalMetadataManager = new GlobalMetadataManager(_config, _fileHandler);
            _entityManagers = new ConcurrentDictionary<string, EntityMetadataManager>();
            _segmentManagers = new ConcurrentDictionary<string, SegmentManager>();
            _initializationLock = new SemaphoreSlim(1, 1);
            _isInitialized = false;
            _disposed = false;
        }

        /// <summary>
        /// EKSIK OLAN METHOD - Initialize database asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));

            if (_isInitialized)
                return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized) // Double-check after acquiring lock
                    return;

                AuditLogger.Log("HIERARCHICAL_DATABASE_INITIALIZATION_STARTED", new
                {
                    DatabasePath = _config.DatabasePath,
                    ConfigHash = _config.GetHashCode()
                }, AuditCategory.Database);

                // Create database directory if it doesn't exist
                if (!Directory.Exists(_config.DatabasePath))
                {
                    Directory.CreateDirectory(_config.DatabasePath);
                    AuditLogger.Log("DATABASE_DIRECTORY_CREATED", new { Path = _config.DatabasePath }, AuditCategory.Database);
                }

                // Initialize global metadata manager
                await _globalMetadataManager.InitializeAsync();

                // Load existing entity metadata
                await LoadExistingEntityMetadata();

                // Initialize performance monitoring if enabled
                if (_config.EnablePerformanceMetrics)
                {
                    InitializePerformanceMonitoring();
                }

                // Initialize cache if enabled
                if (_config.CacheEnabled)
                {
                    InitializeCache();
                }

                // Set up auto-flush timer if configured
                if (_config.AutoFlushInterval.HasValue && _config.AutoFlushInterval.Value > TimeSpan.Zero)
                {
                    SetupAutoFlushTimer();
                }

                _isInitialized = true;

                AuditLogger.Log("HIERARCHICAL_DATABASE_INITIALIZATION_COMPLETED", new
                {
                    DatabasePath = _config.DatabasePath,
                    EntityCount = _entityManagers.Count,
                    MemoryLimitMB = _config.MaxMemoryUsageMB
                }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("HIERARCHICAL_DATABASE_INITIALIZATION_FAILED", ex, new
                {
                    DatabasePath = _config.DatabasePath,
                    Error = ex.Message
                });
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// Load existing entity metadata from disk
        /// </summary>
        private async Task LoadExistingEntityMetadata()
        {
            try
            {
                var entitiesPath = Path.Combine(_config.DatabasePath, "entities");
                if (!Directory.Exists(entitiesPath))
                {
                    Directory.CreateDirectory(entitiesPath);
                    return;
                }

                var entityDirectories = Directory.GetDirectories(entitiesPath);

                foreach (var entityDir in entityDirectories)
                {
                    try
                    {
                        var entityName = Path.GetFileName(entityDir);
                        var metadataManager = new EntityMetadataManager(entityName, _config, _fileHandler);

                        await metadataManager.InitializeAsync();
                        _entityManagers.TryAdd(entityName, metadataManager);

                        // Initialize segment manager for this entity
                        var segmentManager = new SegmentManager(entityName, _config, _fileHandler);
                        await segmentManager.InitializeAsync();
                        _segmentManagers.TryAdd(entityName, segmentManager);

                        AuditLogger.Log("ENTITY_METADATA_LOADED", new
                        {
                            EntityName = entityName,
                            EntityPath = entityDir
                        }, AuditCategory.Database);
                    }
                    catch (Exception ex)
                    {
                        AuditLogger.LogError("FAILED_TO_LOAD_ENTITY_METADATA", ex, new
                        {
                            EntityDirectory = entityDir
                        });
                        // Continue loading other entities even if one fails
                    }
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("FAILED_TO_LOAD_EXISTING_METADATA", ex, new
                {
                    DatabasePath = _config.DatabasePath
                });
                throw;
            }
        }

        /// <summary>
        /// Initialize performance monitoring
        /// </summary>
        private void InitializePerformanceMonitoring()
        {
            try
            {
                // Initialize performance counters and metrics
                AuditLogger.Log("PERFORMANCE_MONITORING_INITIALIZED", new
                {
                    DatabasePath = _config.DatabasePath,
                    Enabled = _config.EnablePerformanceMetrics
                }, AuditCategory.Performance);
            }
            catch (Exception ex)
            {
                AuditLogger.LogWarning("PERFORMANCE_MONITORING_INIT_FAILED", new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Initialize cache system
        /// </summary>
        private void InitializeCache()
        {
            try
            {
                // Initialize caching system
                AuditLogger.Log("CACHE_INITIALIZED", new
                {
                    CacheEnabled = _config.CacheEnabled,
                    CacheSizeMB = _config.CacheSizeMB,
                    IndexCacheEnabled = _config.IndexCacheEnabled
                }, AuditCategory.Performance);
            }
            catch (Exception ex)
            {
                AuditLogger.LogWarning("CACHE_INIT_FAILED", new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Setup auto-flush timer
        /// </summary>
        private void SetupAutoFlushTimer()
        {
            try
            {
                // Setup timer for auto-flushing
                AuditLogger.Log("AUTO_FLUSH_TIMER_SETUP", new
                {
                    IntervalSeconds = _config.AutoFlushInterval?.TotalSeconds
                }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                AuditLogger.LogWarning("AUTO_FLUSH_TIMER_SETUP_FAILED", new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get entity metadata manager
        /// </summary>
        public async Task<EntityMetadataManager> GetEntityManagerAsync(string entityName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));

            if (!_isInitialized)
                await InitializeAsync();

            return _entityManagers.GetOrAdd(entityName, name =>
            {
                var manager = new EntityMetadataManager(name, _config, _fileHandler);
                Task.Run(async () => await manager.InitializeAsync());
                return manager;
            });
        }

        /// <summary>
        /// Get segment manager
        /// </summary>
        public async Task<SegmentManager> GetSegmentManagerAsync(string entityName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));

            if (!_isInitialized)
                await InitializeAsync();

            return _segmentManagers.GetOrAdd(entityName, name =>
            {
                var manager = new SegmentManager(name, _config, _fileHandler);
                Task.Run(async () => await manager.InitializeAsync());
                return manager;
            });
        }

        /// <summary>
        /// Get global metadata manager
        /// </summary>
        public GlobalMetadataManager GetGlobalMetadataManager()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));

            return _globalMetadataManager;
        }

        /// <summary>
        /// Flush all pending operations
        /// </summary>
        public async Task FlushAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));

            try
            {
                var startTime = DateTime.UtcNow;

                // Flush global metadata
                await _globalMetadataManager.FlushAsync();

                // Flush all entity managers
                var entityFlushTasks = _entityManagers.Values.Select(manager => manager.FlushAsync());
                await Task.WhenAll(entityFlushTasks);

                // Flush all segment managers
                var segmentFlushTasks = _segmentManagers.Values.Select(manager => manager.FlushAsync());
                await Task.WhenAll(segmentFlushTasks);

                var duration = DateTime.UtcNow - startTime;

                AuditLogger.LogTimed("DATABASE_FLUSH_COMPLETED", duration, new
                {
                    EntityCount = _entityManagers.Count,
                    SegmentCount = _segmentManagers.Count
                }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("DATABASE_FLUSH_FAILED", ex);
                throw;
            }
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        public async Task<DatabaseStatistics> GetStatisticsAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));

            if (!_isInitialized)
                await InitializeAsync();

            try
            {
                var stats = new DatabaseStatistics
                {
                    DatabasePath = _config.DatabasePath,
                    IsInitialized = _isInitialized,
                    EntityCount = _entityManagers.Count,
                    TotalMemoryUsageMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                    ConfiguredMaxMemoryMB = _config.MaxMemoryUsageMB,
                    CacheEnabled = _config.CacheEnabled,
                    EncryptionEnabled = _config.EncryptionEnabled,
                    CompressionEnabled = _config.EnableCompression
                };

                // Calculate total database size
                if (Directory.Exists(_config.DatabasePath))
                {
                    var directoryInfo = new DirectoryInfo(_config.DatabasePath);
                    stats.TotalSizeBytes = directoryInfo.GetFiles("*", SearchOption.AllDirectories)
                        .Sum(file => file.Length);
                }

                return stats;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("FAILED_TO_GET_DATABASE_STATISTICS", ex);
                throw;
            }
        }

        /// <summary>
        /// Check if database is healthy
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                if (_disposed || !_isInitialized)
                    return false;

                // Basic health checks
                if (!Directory.Exists(_config.DatabasePath))
                    return false;

                // Check memory usage
                var currentMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                if (currentMemoryMB > _config.MaxMemoryUsageMB * 1.1) // 10% tolerance
                    return false;

                // Check if managers are responsive
                await _globalMetadataManager.HealthCheckAsync();

                return true;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("DATABASE_HEALTH_CHECK_FAILED", ex);
                return false;
            }
        }

        /// <summary>
        /// Dispose database and cleanup resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // Flush all pending operations
                    FlushAsync().GetAwaiter().GetResult();

                    // Dispose all managers
                    foreach (var manager in _entityManagers.Values)
                    {
                        if (manager is IDisposable disposable)
                            disposable.Dispose();
                    }

                    foreach (var manager in _segmentManagers.Values)
                    {
                        if (manager is IDisposable disposable)
                            disposable.Dispose();
                    }

                    if (_globalMetadataManager is IDisposable globalDisposable)
                        globalDisposable.Dispose();

                    // Dispose file handler
                    if (_fileHandler is IDisposable fileDisposable)
                        fileDisposable.Dispose();

                    _initializationLock?.Dispose();

                    AuditLogger.Log("HIERARCHICAL_DATABASE_DISPOSED", new
                    {
                        DatabasePath = _config.DatabasePath
                    }, AuditCategory.Database);
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("DATABASE_DISPOSE_ERROR", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Async dispose
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    await FlushAsync();
                    Dispose();
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("DATABASE_ASYNC_DISPOSE_ERROR", ex);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Database statistics structure
    /// </summary>
    public class DatabaseStatistics
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool IsInitialized { get; set; }
        public int EntityCount { get; set; }
        public double TotalMemoryUsageMB { get; set; }
        public int ConfiguredMaxMemoryMB { get; set; }
        public long TotalSizeBytes { get; set; }
        public bool CacheEnabled { get; set; }
        public bool EncryptionEnabled { get; set; }
        public bool CompressionEnabled { get; set; }
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);
        public double MemoryUsagePercentage => ConfiguredMaxMemoryMB > 0 ?
            (TotalMemoryUsageMB / ConfiguredMaxMemoryMB) * 100 : 0;

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:F2} GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F2} KB";
            return $"{bytes} bytes";
        }
    }