using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Factory for creating hierarchical database instances
    /// </summary>
    public static class HierarchicalDatabaseFactory
    {
        /// <summary>
        /// Creates hierarchical database instance asynchronously
        /// </summary>
        /// <param name="config">Database configuration</param>
        /// <returns>Initialized hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateAsync(HierarchicalDatabaseConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Validate configuration
            var validation = config.Validate();
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors);
                throw new ArgumentException($"Invalid configuration: {errors}");
            }

            try
            {
                // Create appropriate file handler based on encryption setting
                IFileHandler fileHandler = config.EncryptionEnabled
                    ? new EncryptedFileHandler(config.Encryption)
                    : new StandardFileHandler();

                // Create database instance
                var database = new HierarchicalDatabase(config, fileHandler);

                // Initialize database
                await database.InitializeAsync();

                AuditLogger.Log("HIERARCHICAL_DATABASE_CREATED", new
                {
                    BasePath = config.BasePath,
                    EncryptionEnabled = config.EncryptionEnabled,
                    MaxSegmentSizeMB = config.MaxSegmentSizeMB,
                    FileExtension = config.FileExtension
                }, category: AuditCategory.System);

                return database;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("HIERARCHICAL_DATABASE_CREATION_FAILED", ex, new
                {
                    BasePath = config.BasePath,
                    EncryptionEnabled = config.EncryptionEnabled
                });
                throw;
            }
        }

        /// <summary>
        /// Creates hierarchical database instance synchronously
        /// </summary>
        /// <param name="config">Database configuration</param>
        /// <returns>Initialized hierarchical database</returns>
        public static HierarchicalDatabase Create(HierarchicalDatabaseConfig config)
        {
            return CreateAsync(config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates hierarchical database with default configuration
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <param name="encryptionEnabled">Whether to enable encryption</param>
        /// <returns>Initialized hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateWithDefaultsAsync(string basePath = "database", bool encryptionEnabled = false)
        {
            var config = new HierarchicalDatabaseConfig
            {
                BasePath = basePath,
                EncryptionEnabled = encryptionEnabled
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Creates hierarchical database for testing purposes
        /// </summary>
        /// <param name="testName">Test name for directory isolation</param>
        /// <param name="encryptionEnabled">Whether to enable encryption</param>
        /// <returns>Initialized test database</returns>
        public static async Task<HierarchicalDatabase> CreateForTestingAsync(string testName, bool encryptionEnabled = false)
        {
            var testPath = Path.Combine(Path.GetTempPath(), "SimpleDataEngine_Tests", testName);

            // Clean up test directory if it exists
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }

            var config = new HierarchicalDatabaseConfig
            {
                BasePath = testPath,
                EncryptionEnabled = encryptionEnabled,
                MaxSegmentSizeMB = 1, // Smaller segments for testing
                MaxRecordsPerSegment = 100,
                EnableCompression = false, // Faster for testing
                EnableRealTimeIndexing = true,
                AutoCleanupDays = 1
            };

            var database = await CreateAsync(config);

            AuditLogger.Log("TEST_DATABASE_CREATED", new
            {
                TestName = testName,
                TestPath = testPath,
                EncryptionEnabled = encryptionEnabled
            }, category: AuditCategory.System);

            return database;
        }

        /// <summary>
        /// Creates hierarchical database with custom encryption
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <param name="encryptionPassword">Custom encryption password</param>
        /// <param name="compressionEnabled">Whether to enable compression</param>
        /// <returns>Initialized hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateWithEncryptionAsync(
            string basePath,
            string encryptionPassword,
            bool compressionEnabled = true)
        {
            if (string.IsNullOrEmpty(encryptionPassword))
                throw new ArgumentException("Encryption password cannot be null or empty", nameof(encryptionPassword));

            var config = new HierarchicalDatabaseConfig
            {
                BasePath = basePath,
                EncryptionEnabled = true,
                EnableCompression = compressionEnabled,
                Encryption = new SimpleDataEngine.Security.EncryptionConfig
                {
                    EnableEncryption = true,
                    CustomPassword = encryptionPassword,
                    CompressBeforeEncrypt = compressionEnabled,
                    EncryptionType = SimpleDataEngine.Security.EncryptionType.AES256,
                    IncludeIntegrityCheck = true
                }
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Creates hierarchical database for production use with optimized settings
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <param name="encryptionEnabled">Whether to enable encryption</param>
        /// <returns>Initialized production database</returns>
        public static async Task<HierarchicalDatabase> CreateForProductionAsync(string basePath, bool encryptionEnabled = true)
        {
            var config = new HierarchicalDatabaseConfig
            {
                BasePath = basePath,
                EncryptionEnabled = encryptionEnabled,
                MaxSegmentSizeMB = 500, // Standard segment size
                MaxRecordsPerSegment = 100000,
                EnableCompression = true,
                EnableRealTimeIndexing = true,
                AutoCleanupDays = 365
            };

            if (encryptionEnabled)
            {
                config.Encryption = new SimpleDataEngine.Security.EncryptionConfig
                {
                    EnableEncryption = true,
                    CompressBeforeEncrypt = true,
                    EncryptionType = SimpleDataEngine.Security.EncryptionType.AES256,
                    IncludeIntegrityCheck = true,
                    KeyDerivationIterations = 10000
                };
            }

            var database = await CreateAsync(config);

            AuditLogger.Log("PRODUCTION_DATABASE_CREATED", new
            {
                BasePath = basePath,
                EncryptionEnabled = encryptionEnabled,
                MaxSegmentSizeMB = config.MaxSegmentSizeMB
            }, category: AuditCategory.System);

            return database;
        }

        /// <summary>
        /// Validates database directory structure
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateDirectoryStructure(string basePath)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                if (string.IsNullOrEmpty(basePath))
                {
                    result.IsValid = false;
                    result.Errors.Add("Base path cannot be null or empty");
                    return result;
                }

                // Check if base directory exists or can be created
                if (!Directory.Exists(basePath))
                {
                    try
                    {
                        Directory.CreateDirectory(basePath);
                        result.Warnings.Add($"Created base directory: {basePath}");
                    }
                    catch (Exception ex)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Cannot create base directory: {ex.Message}");
                        return result;
                    }
                }

                // Check write permissions
                var testFile = Path.Combine(basePath, $"test_{Guid.NewGuid()}.tmp");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"No write permission to base directory: {ex.Message}");
                }

                // Check subdirectories
                var config = new HierarchicalDatabaseConfig { BasePath = basePath };
                var requiredDirs = new[]
                {
                    config.DataModelsPath,
                    config.TempsPath,
                    config.BackupsPath,
                    config.LogsPath
                };

                foreach (var dir in requiredDirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        try
                        {
                            Directory.CreateDirectory(dir);
                            result.Warnings.Add($"Created directory: {dir}");
                        }
                        catch (Exception ex)
                        {
                            result.Warnings.Add($"Could not create directory {dir}: {ex.Message}");
                        }
                    }
                }

                // Check available disk space
                var driveInfo = new DriveInfo(Path.GetPathRoot(basePath));
                var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                if (freeSpaceGB < 1)
                {
                    result.Warnings.Add($"Low disk space: {freeSpaceGB:F2} GB available");
                }
                else if (freeSpaceGB < 0.1)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Insufficient disk space: {freeSpaceGB:F2} GB available");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Directory validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets recommended configuration based on environment
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <param name="environment">Environment type</param>
        /// <returns>Recommended configuration</returns>
        public static HierarchicalDatabaseConfig GetRecommendedConfig(string basePath, DatabaseEnvironment environment)
        {
            return environment switch
            {
                DatabaseEnvironment.Development => new HierarchicalDatabaseConfig
                {
                    BasePath = basePath,
                    EncryptionEnabled = false,
                    MaxSegmentSizeMB = 10, // Smaller for dev
                    MaxRecordsPerSegment = 1000,
                    EnableCompression = false,
                    EnableRealTimeIndexing = true,
                    AutoCleanupDays = 7
                },

                DatabaseEnvironment.Testing => new HierarchicalDatabaseConfig
                {
                    BasePath = basePath,
                    EncryptionEnabled = false,
                    MaxSegmentSizeMB = 1, // Very small for tests
                    MaxRecordsPerSegment = 100,
                    EnableCompression = false,
                    EnableRealTimeIndexing = true,
                    AutoCleanupDays = 1
                },

                DatabaseEnvironment.Staging => new HierarchicalDatabaseConfig
                {
                    BasePath = basePath,
                    EncryptionEnabled = true,
                    MaxSegmentSizeMB = 100,
                    MaxRecordsPerSegment = 50000,
                    EnableCompression = true,
                    EnableRealTimeIndexing = true,
                    AutoCleanupDays = 30
                },

                DatabaseEnvironment.Production => new HierarchicalDatabaseConfig
                {
                    BasePath = basePath,
                    EncryptionEnabled = true,
                    MaxSegmentSizeMB = 500,
                    MaxRecordsPerSegment = 100000,
                    EnableCompression = true,
                    EnableRealTimeIndexing = true,
                    AutoCleanupDays = 365
                },

                _ => new HierarchicalDatabaseConfig { BasePath = basePath }
            };
        }

        /// <summary>
        /// Database environment types
        /// </summary>
        public enum DatabaseEnvironment
        {
            Development,
            Testing,
            Staging,
            Production
        }

        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public bool HasErrors => Errors.Any();
            public bool HasWarnings => Warnings.Any();
        }
    }
}