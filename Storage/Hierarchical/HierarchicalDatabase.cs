using SimpleDataEngine.Audit;
using SimpleDataEngine.Performance;
using SimpleDataEngine.Storage.Hierarchical.Managers;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Hierarchical database implementation - Fixed ambiguity issues
    /// </summary>
    public class HierarchicalDatabase : IDisposable
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly GlobalMetadataManager _globalMetadataManager;
        private readonly Dictionary<string, SegmentManager> _segmentManagers;
        private readonly Dictionary<string, EntityMetadataManager> _entityMetadataManagers;
        private readonly EncryptionService? _encryptionService;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public HierarchicalDatabase(HierarchicalDatabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Validate configuration
            _config.Validate();

            // Initialize file handler based on configuration
            _fileHandler = new StandardFileHandler();

            // Initialize managers
            _globalMetadataManager = new GlobalMetadataManager(_fileHandler, _config.DatabasePath);
            _segmentManagers = new Dictionary<string, SegmentManager>();
            _entityMetadataManagers = new Dictionary<string, Managers.EntityMetadataManager>();

            // Initialize encryption if enabled
            if (_config.EncryptionEnabled && _config.EncryptionConfig != null)
            {
                _encryptionService = new EncryptionService(_config.EncryptionConfig);
            }

            AuditLogger.Log("HIERARCHICAL_DATABASE_CREATED", new
            {
                DatabasePath = _config.DatabasePath,
                EncryptionEnabled = _config.EncryptionEnabled
            }, AuditCategory.Database);
        }

        /// <summary>
        /// Initialize the database
        /// </summary>
        public async Task InitializeAsync()
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("DatabaseInitialize", "Database");

            try
            {
                // Ensure database directory exists
                Directory.CreateDirectory(_config.DatabasePath);

                // Initialize global metadata
                await _globalMetadataManager.InitializeAsync();

                operation.MarkSuccess();

                AuditLogger.Log("HIERARCHICAL_DATABASE_INITIALIZED", new
                {
                    DatabasePath = _config.DatabasePath
                }, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("HIERARCHICAL_DATABASE_INIT_FAILED", ex);
                throw;
            }
        }

        /// <summary>
        /// Get or create entity manager
        /// </summary>
        public async Task<Managers.EntityMetadataManager> GetEntityManagerAsync(string entityName)
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                if (_entityMetadataManagers.TryGetValue(entityName, out var existingManager))
                {
                    return existingManager;
                }

                var manager = new Managers.EntityMetadataManager(_fileHandler, _config.DatabasePath, entityName);
                _entityMetadataManagers[entityName] = manager;
                return manager;
            }
        }

        /// <summary>
        /// Get or create segment manager
        /// </summary>
        public async Task<SegmentManager> GetSegmentManagerAsync(string entityName)
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                if (_segmentManagers.TryGetValue(entityName, out var existingManager))
                {
                    return existingManager;
                }

                var entityMetadataManager = GetEntityManagerAsync(entityName).Result;
                var manager = new SegmentManager(entityName, _config, _fileHandler);

                // Initialize the segment manager
                manager.InitializeAsync().Wait();

                _segmentManagers[entityName] = manager;
                return manager;
            }
        }

        /// <summary>
        /// Store entity data
        /// </summary>
        public async Task<string> StoreAsync<T>(string entityName, T data)
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("StoreEntity", "Database");

            try
            {
                var segmentManager = await GetSegmentManagerAsync(entityName);
                var entityManager = await GetEntityManagerAsync(entityName);

                // Serialize data
                var serializedData = System.Text.Json.JsonSerializer.Serialize(data);

                // Encrypt if needed
                if (_encryptionService != null)
                {
                    serializedData = await _encryptionService.EncryptAsync(serializedData);
                }

                // Store in segment
                var id = await segmentManager.StoreAsync(serializedData);

                // Update entity metadata
                await entityManager.UpdateAsync();

                operation.MarkSuccess();

                AuditLogger.Log("ENTITY_STORED", new
                {
                    EntityName = entityName,
                    EntityId = id,
                    DataSize = serializedData.Length
                }, AuditCategory.Database);

                return id;
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("ENTITY_STORE_FAILED", ex, new { EntityName = entityName });
                throw;
            }
        }

        /// <summary>
        /// Retrieve entity data
        /// </summary>
        public async Task<T?> RetrieveAsync<T>(string entityName, string id)
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("RetrieveEntity", "Database");

            try
            {
                var segmentManager = await GetSegmentManagerAsync(entityName);

                // Retrieve from segment
                var serializedData = await segmentManager.RetrieveAsync(id);

                if (serializedData == null)
                {
                    operation.MarkSuccess();
                    return default(T);
                }

                // Decrypt if needed
                if (_encryptionService != null)
                {
                    serializedData = await _encryptionService.DecryptAsync(serializedData);
                }

                // Deserialize
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(serializedData);

                operation.MarkSuccess();

                AuditLogger.Log("ENTITY_RETRIEVED", new
                {
                    EntityName = entityName,
                    EntityId = id
                }, AuditCategory.Database);

                return data;
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("ENTITY_RETRIEVE_FAILED", ex, new { EntityName = entityName, EntityId = id });
                throw;
            }
        }

        /// <summary>
        /// Flush all pending operations
        /// </summary>
        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("DatabaseFlush", "Database");

            try
            {
                var flushTasks = new List<Task>();

                // Flush global metadata
                await _globalMetadataManager.SaveAsync();

                // Flush all segment managers
                lock (_lock)
                {
                    foreach (var segmentManager in _segmentManagers.Values)
                    {
                        flushTasks.Add(segmentManager.FlushAsync());
                    }
                }

                if (flushTasks.Any())
                {
                    await Task.WhenAll(flushTasks);
                }

                operation.MarkSuccess();

                AuditLogger.Log("DATABASE_FLUSHED", null, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("DATABASE_FLUSH_FAILED", ex);
                throw;
            }
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        public async Task<DatabaseStatistics> GetStatisticsAsync()
        {
            ThrowIfDisposed();

            using var operation = PerformanceTracker.StartOperation("GetDatabaseStatistics", "Database");

            try
            {
                var stats = new DatabaseStatistics
                {
                    DatabasePath = _config.DatabasePath,
                    EncryptionEnabled = _config.EncryptionEnabled,
                    CompressionEnabled = _config.EnableCompression,
                    TotalEntities = _segmentManagers.Count,
                    CollectedAt = DateTime.UtcNow
                };

                // Calculate total size
                var databaseDir = new DirectoryInfo(_config.DatabasePath);
                if (databaseDir.Exists)
                {
                    stats.TotalSizeBytes = databaseDir.GetFiles("*", SearchOption.AllDirectories)
                        .Sum(f => f.Length);
                }

                operation.MarkSuccess();
                return stats;
            }
            catch (Exception ex)
            {
                operation.MarkFailure();
                AuditLogger.LogError("GET_DATABASE_STATISTICS_FAILED", ex);
                throw;
            }
        }

        /// <summary>
        /// Health check for the database
        /// </summary>
        public async Task<HealthCheckResult> HealthCheckAsync()
        {
            var result = new HealthCheckResult
            {
                Name = "HierarchicalDatabase",
                Category = HealthCategory.Storage
            };

            try
            {
                // Check if database directory exists and is writable
                var testFile = Path.Combine(_config.DatabasePath, $"healthcheck_{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);

                // Check global metadata
                await _globalMetadataManager.LoadAsync();

                result.Status = HealthStatus.Healthy;
                result.Message = "Database is healthy and accessible";

                var stats = await GetStatisticsAsync();
                result.Data["TotalEntities"] = stats.TotalEntities;
                result.Data["TotalSizeBytes"] = stats.TotalSizeBytes;
                result.Data["EncryptionEnabled"] = stats.EncryptionEnabled;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = $"Database health check failed: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Flush all pending operations
                FlushAsync().Wait(TimeSpan.FromSeconds(30));

                // Dispose managers
                lock (_lock)
                {
                    foreach (var manager in _segmentManagers.Values)
                    {
                        manager.Dispose();
                    }
                    _segmentManagers.Clear();

                    foreach (var manager in _entityMetadataManagers.Values)
                    {
                        manager.Dispose();
                    }
                    _entityMetadataManagers.Clear();
                }

                _globalMetadataManager.Dispose();
                _encryptionService?.Dispose();

                _disposed = true;

                AuditLogger.Log("HIERARCHICAL_DATABASE_DISPOSED", null, AuditCategory.Database);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("HIERARCHICAL_DATABASE_DISPOSE_FAILED", ex);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));
            }
        }
    }
}