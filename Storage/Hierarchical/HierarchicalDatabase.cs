using SimpleDataEngine.Audit;
using SimpleDataEngine.Storage.Hierarchical.Models;
using SimpleDataEngine.Storage.Hierarchical.Managers;
using System.Collections.Concurrent;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Main hierarchical database manager - coordinates all operations
    /// </summary>
    public class HierarchicalDatabase : IDisposable
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly GlobalMetadataManager _globalMetadataManager;
        private readonly ConcurrentDictionary<string, EntityMetadataManager> _entityManagers;
        private readonly ConcurrentDictionary<string, SegmentManager> _segmentManagers;
        private readonly SemaphoreSlim _initializationLock;
        private bool _isInitialized;
        private bool _disposed;

        public HierarchicalDatabase(HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _globalMetadataManager = new GlobalMetadataManager(config, fileHandler);
            _entityManagers = new ConcurrentDictionary<string, EntityMetadataManager>();
            _segmentManagers = new ConcurrentDictionary<string, SegmentManager>();
            _initializationLock = new SemaphoreSlim(1, 1);
        }

        #region Initialization

        /// <summary>
        /// Initialize database - create directories, load metadata
        /// </summary>
        public async Task InitializeAsync()
        {
            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                // Create directory structure
                await CreateDirectoryStructureAsync();

                // Initialize global metadata
                await _globalMetadataManager.InitializeAsync();

                // Load existing entities
                await LoadExistingEntitiesAsync();

                _isInitialized = true;

                await AuditLogger.LogAsync("Database initialized successfully",
                    new { BasePath = _config.BasePath, EncryptionEnabled = _config.EncryptionEnabled });
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Database initialization failed", ex);
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private async Task CreateDirectoryStructureAsync()
        {
            var directories = new[]
            {
                _config.DataModelsPath,
                _config.TempsPath,
                _config.BackupsPath,
                _config.LogsPath
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        private async Task LoadExistingEntitiesAsync()
        {
            if (!Directory.Exists(_config.DataModelsPath)) return;

            var entityDirs = Directory.GetDirectories(_config.DataModelsPath);

            foreach (var entityDir in entityDirs)
            {
                var entityName = Path.GetFileName(entityDir);
                await EnsureEntityManagerAsync(entityName);
            }
        }

        #endregion

        #region Entity Management

        /// <summary>
        /// Get or create entity storage
        /// </summary>
        public async Task<HierarchicalStorage<T>> GetStorageAsync<T>(string entityName) where T : class
        {
            ThrowIfDisposed();
            await EnsureInitializedAsync();

            // Ensure entity manager exists
            await EnsureEntityManagerAsync(entityName);

            // Create storage instance
            return new HierarchicalStorage<T>(_config, _fileHandler, entityName);
        }

        /// <summary>
        /// Get all registered entity names
        /// </summary>
        public async Task<IEnumerable<string>> GetEntityNamesAsync()
        {
            ThrowIfDisposed();
            await EnsureInitializedAsync();

            var globalMetadata = await _globalMetadataManager.GetMetadataAsync();
            return globalMetadata?.RegisteredEntities?.Keys ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Register a new entity type
        /// </summary>
        public async Task RegisterEntityAsync<T>(string entityName) where T : class
        {
            ThrowIfDisposed();
            await EnsureInitializedAsync();

            await EnsureEntityManagerAsync(entityName);

            // Update global metadata
            await _globalMetadataManager.UpdateEntityRegistrationAsync(entityName, typeof(T));

            await AuditLogger.LogAsync($"Entity registered: {entityName}", new { EntityType = typeof(T).Name });
        }

        private async Task EnsureEntityManagerAsync(string entityName)
        {
            if (_entityManagers.ContainsKey(entityName)) return;

            var entityManager = new EntityMetadataManager(_config, _fileHandler, entityName);
            await entityManager.InitializeAsync();

            var segmentManager = new SegmentManager(_config, _fileHandler, entityName);
            await segmentManager.InitializeAsync();

            _entityManagers.TryAdd(entityName, entityManager);
            _segmentManagers.TryAdd(entityName, segmentManager);
        }

        #endregion

        #region Global Operations

        /// <summary>
        /// Get database statistics
        /// </summary>
        public async Task<DatabaseStatistics> GetStatisticsAsync()
        {
            ThrowIfDisposed();
            await EnsureInitializedAsync();

            var globalMetadata = await _globalMetadataManager.GetMetadataAsync();
            var stats = new DatabaseStatistics
            {
                DatabaseId = globalMetadata.DatabaseId,
                CreatedAt = globalMetadata.CreatedAt,
                LastModified = globalMetadata.LastModified,
                TotalEntities = globalMetadata.RegisteredEntities?.Count ?? 0,
                EncryptionEnabled = _config.EncryptionEnabled,
                BasePath = _config.BasePath
            };

            // Collect entity statistics
            var entityStats = new List<EntityStatistics>();
            foreach (var entityName in await GetEntityNamesAsync())
            {
                if (_entityManagers.TryGetValue(entityName, out var entityManager))
                {
                    var entityMetadata = await entityManager.GetMetadataAsync();
                    if (entityMetadata != null)
                    {
                        entityStats.Add(new EntityStatistics
                        {
                            EntityName = entityName,
                            TotalRecords = entityMetadata.TotalRecords,
                            TotalSegments = entityMetadata.Segments?.Count ?? 0,
                            TotalSizeBytes = entityMetadata.Segments?.Sum(s => s.FileSizeBytes) ?? 0,
                            LastModified = entityMetadata.LastModified
                        });
                    }
                }
            }

            stats.EntityStatistics = entityStats;
            return stats;
        }

        /// <summary>
        /// Perform database maintenance (cleanup, optimization)
        /// </summary>
        public async Task MaintenanceAsync(bool forceFullMaintenance = false)
        {
            ThrowIfDisposed();
            await EnsureInitializedAsync();

            await AuditLogger.LogAsync("Database maintenance started", new { ForceFullMaintenance = forceFullMaintenance });

            try
            {
                // Clean up old temporary files
                await CleanupTemporaryFilesAsync();

                // Clean up old segments if auto-cleanup is enabled
                if (_config.AutoCleanupDays > 0)
                {
                    await CleanupOldSegmentsAsync();
                }

                // Optimize indexes if needed
                if (forceFullMaintenance)
                {
                    await OptimizeIndexesAsync();
                }

                // Update global metadata
                await _globalMetadataManager.UpdateMaintenanceInfoAsync();

                await AuditLogger.LogAsync("Database maintenance completed successfully");
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Database maintenance failed", ex);
                throw;
            }
        }

        private async Task CleanupTemporaryFilesAsync()
        {
            if (!Directory.Exists(_config.TempsPath)) return;

            var tempFiles = Directory.GetFiles(_config.TempsPath, "*", SearchOption.AllDirectories);
            var cutoffDate = DateTime.UtcNow.AddHours(-24); // Clean files older than 24 hours

            foreach (var file in tempFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                    }
                }
            }
        }

        private async Task CleanupOldSegmentsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_config.AutoCleanupDays);

            foreach (var segmentManager in _segmentManagers.Values)
            {
                await segmentManager.CleanupOldSegmentsAsync(cutoffDate);
            }
        }

        private async Task OptimizeIndexesAsync()
        {
            foreach (var entityName in await GetEntityNamesAsync())
            {
                if (_entityManagers.TryGetValue(entityName, out var entityManager))
                {
                    await entityManager.RebuildIndexesAsync();
                }
            }
        }

        #endregion

        #region Backup & Restore

        /// <summary>
        /// Create full database backup
        /// </summary>
        public async Task<string> CreateBackupAsync(string backupName = null)
        {
            ThrowIfDisposed();
            await EnsureInitializedAsync();

            backupName ??= $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var backupPath = Path.Combine(_config.BackupsPath, backupName);

            await AuditLogger.LogAsync($"Creating backup: {backupName}");

            try
            {
                Directory.CreateDirectory(backupPath);

                // Copy global metadata
                var globalMetadataFile = Path.Combine(_config.BasePath, "global_metadata" + _config.FileExtension);
                if (File.Exists(globalMetadataFile))
                {
                    var backupMetadataFile = Path.Combine(backupPath, Path.GetFileName(globalMetadataFile));
                    File.Copy(globalMetadataFile, backupMetadataFile);
                }

                // Copy all entity data
                if (Directory.Exists(_config.DataModelsPath))
                {
                    var backupDataPath = Path.Combine(backupPath, "datamodels");
                    await CopyDirectoryAsync(_config.DataModelsPath, backupDataPath);
                }

                await AuditLogger.LogAsync($"Backup created successfully: {backupName}");
                return backupPath;
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Backup creation failed: {backupName}", ex);
                throw;
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            // Copy files
            var files = Directory.GetFiles(sourceDir);
            foreach (var file in files)
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile);
            }

            // Copy subdirectories
            var dirs = Directory.GetDirectories(sourceDir);
            foreach (var dir in dirs)
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, targetSubDir);
            }
        }

        #endregion

        #region Utilities

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HierarchicalDatabase));
            }
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
                _initializationLock?.Dispose();
                _fileHandler?.Dispose();

                // Dispose all entity managers
                foreach (var manager in _entityManagers.Values)
                {
                    manager?.Dispose();
                }
                _entityManagers.Clear();

                // Dispose all segment managers
                foreach (var manager in _segmentManagers.Values)
                {
                    manager?.Dispose();
                }
                _segmentManagers.Clear();

                _disposed = true;
            }
        }

        #endregion
    }

    #region Supporting Models



    /// <summary>
    /// Individual entity statistics
    /// </summary>
    #endregion
}