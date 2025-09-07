using SimpleDataEngine.Audit;
using System.Text.Json;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Global metadata manager for database-wide operations
    /// </summary>
    public class GlobalMetadataManager : IDisposable
    {
        private readonly IFileHandler _fileHandler;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _metadataPath;
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Initializes global metadata manager
        /// </summary>
        /// <param name="fileHandler">File handler for I/O operations</param>
        /// <param name="dataModelsPath">Data models directory path</param>
        public GlobalMetadataManager(IFileHandler fileHandler, string dataModelsPath)
        {
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _metadataPath = Path.Combine(dataModelsPath, $"metadata{_fileHandler.FileExtension}");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Loads global metadata from file
        /// </summary>
        /// <returns>Global metadata or new instance if file doesn't exist</returns>
        public async Task<GlobalMetadata> LoadAsync()
        {
            ThrowIfDisposed();

            try
            {
                var content = await _fileHandler.ReadTextAsync(_metadataPath);
                if (string.IsNullOrEmpty(content))
                {
                    var newMetadata = CreateDefaultMetadata();
                    await SaveAsync(newMetadata);
                    return newMetadata;
                }

                var metadata = JsonSerializer.Deserialize<GlobalMetadata>(content, _jsonOptions);
                return metadata ?? CreateDefaultMetadata();
            }
            catch (JsonException ex)
            {
                AuditLogger.LogError("GLOBAL_METADATA_LOAD_JSON_ERROR", ex, new { MetadataPath = _metadataPath });
                throw new InvalidOperationException($"Failed to parse global metadata: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("GLOBAL_METADATA_LOAD_ERROR", ex, new { MetadataPath = _metadataPath });
                throw new InvalidOperationException($"Failed to load global metadata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves global metadata to file
        /// </summary>
        /// <param name="metadata">Metadata to save</param>
        public async Task SaveAsync(GlobalMetadata metadata)
        {
            ThrowIfDisposed();

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            lock (_lock)
            {
                try
                {
                    metadata.LastUpdated = DateTime.Now;
                    var content = JsonSerializer.Serialize(metadata, _jsonOptions);

                    // Async save
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _fileHandler.WriteTextAsync(_metadataPath, content);

                            AuditLogger.Log("GLOBAL_METADATA_SAVED", new
                            {
                                MetadataPath = _metadataPath,
                                TotalEntities = metadata.TotalEntities,
                                TotalRecords = metadata.TotalRecords,
                                TotalSizeMB = metadata.TotalSizeMB
                            }, category: AuditCategory.Data);
                        }
                        catch (Exception ex)
                        {
                            AuditLogger.LogError("GLOBAL_METADATA_SAVE_ERROR", ex, new { MetadataPath = _metadataPath });
                        }
                    });
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("GLOBAL_METADATA_SERIALIZE_ERROR", ex);
                    throw new InvalidOperationException($"Failed to serialize global metadata: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Updates entity information in global metadata
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <param name="recordCount">Record count</param>
        /// <param name="segmentCount">Segment count</param>
        /// <param name="sizeMB">Size in MB</param>
        public async Task UpdateEntityInfoAsync(string entityName, long recordCount, int segmentCount, long sizeMB)
        {
            ThrowIfDisposed();

            var metadata = await LoadAsync();

            var entityInfo = metadata.Entities.FirstOrDefault(e => e.Name == entityName);
            if (entityInfo == null)
            {
                entityInfo = new GlobalMetadata.EntityInfo
                {
                    Name = entityName,
                    CreatedAt = DateTime.Now
                };
                metadata.Entities.Add(entityInfo);
                metadata.TotalEntities = metadata.Entities.Count;
            }

            // Update entity stats
            var oldRecordCount = entityInfo.RecordCount;
            var oldSizeMB = entityInfo.SizeMB;

            entityInfo.RecordCount = recordCount;
            entityInfo.SegmentCount = segmentCount;
            entityInfo.SizeMB = sizeMB;
            entityInfo.LastUpdated = DateTime.Now;

            // Update global totals
            metadata.TotalRecords = metadata.TotalRecords - oldRecordCount + recordCount;
            metadata.TotalSizeMB = metadata.TotalSizeMB - oldSizeMB + sizeMB;

            await SaveAsync(metadata);
        }

        /// <summary>
        /// Removes entity from global metadata
        /// </summary>
        /// <param name="entityName">Entity name to remove</param>
        public async Task RemoveEntityAsync(string entityName)
        {
            ThrowIfDisposed();

            var metadata = await LoadAsync();
            var entityInfo = metadata.Entities.FirstOrDefault(e => e.Name == entityName);

            if (entityInfo != null)
            {
                metadata.Entities.Remove(entityInfo);
                metadata.TotalEntities = metadata.Entities.Count;
                metadata.TotalRecords -= entityInfo.RecordCount;
                metadata.TotalSizeMB -= entityInfo.SizeMB;

                await SaveAsync(metadata);

                AuditLogger.Log("GLOBAL_METADATA_ENTITY_REMOVED", new
                {
                    EntityName = entityName,
                    RemovedRecords = entityInfo.RecordCount,
                    RemovedSizeMB = entityInfo.SizeMB
                }, category: AuditCategory.Data);
            }
        }

        /// <summary>
        /// Gets entity information from global metadata
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <returns>Entity information or null if not found</returns>
        public async Task<GlobalMetadata.EntityInfo> GetEntityInfoAsync(string entityName)
        {
            ThrowIfDisposed();

            var metadata = await LoadAsync();
            return metadata.Entities.FirstOrDefault(e => e.Name == entityName);
        }

        /// <summary>
        /// Gets all entity names
        /// </summary>
        /// <returns>List of entity names</returns>
        public async Task<List<string>> GetEntityNamesAsync()
        {
            ThrowIfDisposed();

            var metadata = await LoadAsync();
            return metadata.Entities.Select(e => e.Name).ToList();
        }

        /// <summary>
        /// Updates database configuration snapshot
        /// </summary>
        /// <param name="config">Database configuration</param>
        public async Task UpdateConfigSnapshotAsync(HierarchicalDatabaseConfig config)
        {
            ThrowIfDisposed();

            var metadata = await LoadAsync();
            metadata.ConfigSnapshot = new GlobalMetadata.DatabaseConfigSnapshot
            {
                MaxSegmentSizeMB = config.MaxSegmentSizeMB,
                MaxRecordsPerSegment = config.MaxRecordsPerSegment,
                EnableCompression = config.EnableCompression,
                EnableRealTimeIndexing = config.EnableRealTimeIndexing,
                AutoCleanupDays = config.AutoCleanupDays
            };

            await SaveAsync(metadata);
        }

        /// <summary>
        /// Checks if metadata file exists
        /// </summary>
        /// <returns>True if metadata file exists</returns>
        public async Task<bool> ExistsAsync()
        {
            ThrowIfDisposed();
            return await _fileHandler.ExistsAsync(_metadataPath);
        }

        /// <summary>
        /// Gets metadata file path
        /// </summary>
        /// <returns>Metadata file path</returns>
        public string GetMetadataPath()
        {
            return _metadataPath;
        }

        /// <summary>
        /// Creates backup of metadata file
        /// </summary>
        /// <param name="backupPath">Backup file path</param>
        public async Task CreateBackupAsync(string backupPath)
        {
            ThrowIfDisposed();

            if (await _fileHandler.ExistsAsync(_metadataPath))
            {
                await _fileHandler.CopyAsync(_metadataPath, backupPath);

                AuditLogger.Log("GLOBAL_METADATA_BACKUP_CREATED", new
                {
                    OriginalPath = _metadataPath,
                    BackupPath = backupPath
                }, category: AuditCategory.Backup);
            }
        }

        /// <summary>
        /// Validates metadata integrity
        /// </summary>
        /// <returns>Validation result</returns>
        public async Task<ValidationResult> ValidateAsync()
        {
            ThrowIfDisposed();

            var result = new ValidationResult { IsValid = true };

            try
            {
                var metadata = await LoadAsync();

                // Validate basic structure
                if (string.IsNullOrEmpty(metadata.DatabaseVersion))
                {
                    result.IsValid = false;
                    result.Errors.Add("Database version is missing");
                }

                // Validate entity consistency
                var calculatedTotalRecords = metadata.Entities.Sum(e => e.RecordCount);
                if (metadata.TotalRecords != calculatedTotalRecords)
                {
                    result.Warnings.Add($"Total records mismatch: metadata={metadata.TotalRecords}, calculated={calculatedTotalRecords}");
                }

                var calculatedTotalSize = metadata.Entities.Sum(e => e.SizeMB);
                if (Math.Abs(metadata.TotalSizeMB - calculatedTotalSize) > 1) // 1MB tolerance
                {
                    result.Warnings.Add($"Total size mismatch: metadata={metadata.TotalSizeMB}MB, calculated={calculatedTotalSize}MB");
                }

                // Validate entity count
                if (metadata.TotalEntities != metadata.Entities.Count)
                {
                    result.Warnings.Add($"Entity count mismatch: metadata={metadata.TotalEntities}, actual={metadata.Entities.Count}");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates default metadata instance
        /// </summary>
        /// <returns>Default global metadata</returns>
        private GlobalMetadata CreateDefaultMetadata()
        {
            return new GlobalMetadata
            {
                DatabaseVersion = "1.0.0",
                CreatedAt = DateTime.Now,
                LastUpdated = DateTime.Now,
                EncryptionEnabled = _fileHandler.FileExtension == ".sde",
                TotalEntities = 0,
                TotalRecords = 0,
                TotalSizeMB = 0,
                Entities = new List<GlobalMetadata.EntityInfo>()
            };
        }

        /// <summary>
        /// Throws ObjectDisposedException if manager is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GlobalMetadataManager));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }
    }
}