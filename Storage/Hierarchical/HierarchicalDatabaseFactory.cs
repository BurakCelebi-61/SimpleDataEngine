using SimpleDataEngine.Audit;
using SimpleDataEngine.Security;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Factory for creating hierarchical database instances - Tüm hatalar düzeltildi
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
                    ? new EncryptedFileHandler(config.EncryptionConfig)
                    : new StandardFileHandler();

                // Create database instance
                var database = new HierarchicalDatabase(config, fileHandler);

                // Initialize the database - InitializeAsync metodu eklendi
                await database.InitializeAsync();

                // Log successful creation
                AuditLogger.Log("HIERARCHICAL_DATABASE_CREATED", new
                {
                    DatabasePath = config.DatabasePath,
                    EncryptionEnabled = config.EncryptionEnabled,
                    MaxMemoryUsageMB = config.MaxMemoryUsageMB
                }, AuditCategory.Database);

                return database;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("HIERARCHICAL_DATABASE_CREATION_FAILED", ex, new
                {
                    DatabasePath = config.DatabasePath,
                    ConfigDetails = config
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
        /// Creates a hierarchical database with default configuration
        /// </summary>
        /// <param name="databasePath">Path to the database directory</param>
        /// <returns>Initialized hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateDefaultAsync(string databasePath)
        {
            var config = new HierarchicalDatabaseConfig
            {
                DatabasePath = databasePath,
                EncryptionEnabled = false,
                MaxMemoryUsageMB = 512,
                AutoFlushInterval = TimeSpan.FromSeconds(30),
                EnableCompression = true,
                MaxSegmentSizeMB = 64
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Creates an encrypted hierarchical database
        /// </summary>
        /// <param name="databasePath">Path to the database directory</param>
        /// <param name="encryptionKey">Encryption key</param>
        /// <returns>Initialized encrypted hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateEncryptedAsync(string databasePath, string encryptionKey)
        {
            var encryptionConfig = new EncryptionConfig
            {
                EncryptionKey = encryptionKey,
                EncryptionType = EncryptionType.AES256,
                EnableEncryption = true,
                CompressBeforeEncrypt = true
            };

            var config = new HierarchicalDatabaseConfig
            {
                DatabasePath = databasePath,
                EncryptionEnabled = true,
                EncryptionConfig = encryptionConfig,
                MaxMemoryUsageMB = 512,
                AutoFlushInterval = TimeSpan.FromSeconds(30),
                EnableCompression = false, // Compression handled by encryption
                MaxSegmentSizeMB = 64
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Creates a high-performance hierarchical database configuration
        /// </summary>
        /// <param name="databasePath">Path to the database directory</param>
        /// <param name="maxMemoryMB">Maximum memory usage in MB</param>
        /// <returns>Initialized high-performance hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateHighPerformanceAsync(string databasePath, int maxMemoryMB = 2048)
        {
            var config = new HierarchicalDatabaseConfig
            {
                DatabasePath = databasePath,
                EncryptionEnabled = false,
                MaxMemoryUsageMB = maxMemoryMB,
                AutoFlushInterval = TimeSpan.FromMinutes(5), // Less frequent flushes
                EnableCompression = false, // Disabled for performance
                MaxSegmentSizeMB = 256, // Larger segments
                CacheEnabled = true,
                CacheSizeMB = maxMemoryMB / 4, // 25% of memory for cache
                IndexCacheEnabled = true
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Creates a minimal footprint hierarchical database
        /// </summary>
        /// <param name="databasePath">Path to the database directory</param>
        /// <returns>Initialized minimal hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateMinimalAsync(string databasePath)
        {
            var config = new HierarchicalDatabaseConfig
            {
                DatabasePath = databasePath,
                EncryptionEnabled = false,
                MaxMemoryUsageMB = 64, // Minimal memory usage
                AutoFlushInterval = TimeSpan.FromSeconds(10), // Frequent flushes
                EnableCompression = true, // Save disk space
                MaxSegmentSizeMB = 8, // Small segments
                CacheEnabled = false, // No cache to save memory
                IndexCacheEnabled = false
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Creates a development/testing hierarchical database
        /// </summary>
        /// <param name="databasePath">Path to the database directory</param>
        /// <returns>Initialized development hierarchical database</returns>
        public static async Task<HierarchicalDatabase> CreateDevelopmentAsync(string databasePath)
        {
            var config = new HierarchicalDatabaseConfig
            {
                DatabasePath = databasePath,
                EncryptionEnabled = false,
                MaxMemoryUsageMB = 256,
                AutoFlushInterval = TimeSpan.FromSeconds(5), // Quick flushes for testing
                EnableCompression = false, // Faster I/O for development
                MaxSegmentSizeMB = 32,
                CacheEnabled = true,
                CacheSizeMB = 64,
                IndexCacheEnabled = true,
                EnableDetailedLogging = true, // Extra logging for debugging
                EnablePerformanceMetrics = true
            };

            return await CreateAsync(config);
        }

        /// <summary>
        /// Validates if a database exists and is accessible at the given path
        /// </summary>
        /// <param name="databasePath">Path to check</param>
        /// <returns>True if database exists and is accessible</returns>
        public static bool DatabaseExists(string databasePath)
        {
            try
            {
                if (string.IsNullOrEmpty(databasePath))
                    return false;

                if (!Directory.Exists(databasePath))
                    return false;

                // Check for required database files
                var metadataPath = Path.Combine(databasePath, "metadata.json");
                var configPath = Path.Combine(databasePath, "config.json");

                return File.Exists(metadataPath) || File.Exists(configPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets database information without fully opening it
        /// </summary>
        /// <param name="databasePath">Database path</param>
        /// <returns>Database information</returns>
        public static async Task<DatabaseInfo> GetDatabaseInfoAsync(string databasePath)
        {
            try
            {
                var info = new DatabaseInfo
                {
                    DatabasePath = databasePath,
                    Exists = DatabaseExists(databasePath),
                    CreatedDate = Directory.GetCreationTime(databasePath),
                    LastModifiedDate = Directory.GetLastWriteTime(databasePath)
                };

                if (info.Exists)
                {
                    // Calculate total size
                    var directoryInfo = new DirectoryInfo(databasePath);
                    info.TotalSizeBytes = directoryInfo.GetFiles("*", SearchOption.AllDirectories)
                        .Sum(file => file.Length);

                    // Check for encryption
                    var configPath = Path.Combine(databasePath, "config.json");
                    if (File.Exists(configPath))
                    {
                        var configJson = await File.ReadAllTextAsync(configPath);
                        // Simple check for encryption config
                        info.IsEncrypted = configJson.Contains("\"EncryptionEnabled\":true", StringComparison.OrdinalIgnoreCase);
                    }

                    // Count entity types
                    var entitiesPath = Path.Combine(databasePath, "entities");
                    if (Directory.Exists(entitiesPath))
                    {
                        info.EntityCount = Directory.GetDirectories(entitiesPath).Length;
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                return new DatabaseInfo
                {
                    DatabasePath = databasePath,
                    Exists = false,
                    Error = ex.Message
                };
            }
        }
    }

    /// <summary>
    /// Database information structure
    /// </summary>
    public class DatabaseInfo
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public long TotalSizeBytes { get; set; }
        public int EntityCount { get; set; }
        public string? Error { get; set; }

        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);

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
}