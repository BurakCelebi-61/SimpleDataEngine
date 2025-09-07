using SimpleDataEngine.Security;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Configuration for hierarchical database
    /// </summary>
    public class HierarchicalDatabaseConfig
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool EncryptionEnabled { get; set; } = false;
        public EncryptionConfig? EncryptionConfig { get; set; }
        public bool EnableCompression { get; set; } = false;
        public int MaxSegmentSizeMB { get; set; } = 100;
        public int MaxMemoryUsageMB { get; set; } = 512;
        public TimeSpan? FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
        public bool AutoFlush { get; set; } = true;
        public int CacheSize { get; set; } = 1000;
        public bool EnableIndexing { get; set; } = true;
        public string BackupDirectory { get; set; } = string.Empty;
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Validate configuration
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(DatabasePath))
            {
                throw new ArgumentException("Database path cannot be null or empty", nameof(DatabasePath));
            }

            if (MaxSegmentSizeMB <= 0)
            {
                throw new ArgumentException("Max segment size must be greater than 0", nameof(MaxSegmentSizeMB));
            }

            if (MaxMemoryUsageMB <= 0)
            {
                throw new ArgumentException("Max memory usage must be greater than 0", nameof(MaxMemoryUsageMB));
            }

            if (EncryptionEnabled && EncryptionConfig == null)
            {
                throw new ArgumentException("Encryption config must be provided when encryption is enabled", nameof(EncryptionConfig));
            }

            if (CacheSize < 0)
            {
                throw new ArgumentException("Cache size cannot be negative", nameof(CacheSize));
            }

            if (FlushInterval.HasValue && FlushInterval.Value <= TimeSpan.Zero)
            {
                throw new ArgumentException("Flush interval must be positive", nameof(FlushInterval));
            }
        }

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static HierarchicalDatabaseConfig CreateDefault(string databasePath)
        {
            return new HierarchicalDatabaseConfig
            {
                DatabasePath = databasePath,
                EncryptionEnabled = false,
                EnableCompression = false,
                MaxSegmentSizeMB = 100,
                MaxMemoryUsageMB = 512,
                FlushInterval = TimeSpan.FromSeconds(30),
                AutoFlush = true,
                CacheSize = 1000,
                EnableIndexing = true,
                BackupDirectory = Path.Combine(databasePath, "backups"),
                EnableMetrics = true
            };
        }

        /// <summary>
        /// Create configuration with encryption
        /// </summary>
        public static HierarchicalDatabaseConfig CreateWithEncryption(string databasePath, EncryptionConfig encryptionConfig)
        {
            var config = CreateDefault(databasePath);
            config.EncryptionEnabled = true;
            config.EncryptionConfig = encryptionConfig;
            return config;
        }
    }
}