using SimpleDataEngine.Security;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Hierarchical database configuration
    /// </summary>
    public class HierarchicalDatabaseConfig
    {
        /// <summary>
        /// Base path for database storage
        /// </summary>
        public string BasePath { get; set; } = "database";

        /// <summary>
        /// Whether encryption is enabled for all files
        /// </summary>
        public bool EncryptionEnabled { get; set; } = false;

        /// <summary>
        /// Encryption configuration settings
        /// </summary>
        public EncryptionConfig Encryption { get; set; } = new();

        /// <summary>
        /// Maximum segment size in MB before creating new segment
        /// </summary>
        public int MaxSegmentSizeMB { get; set; } = 500;

        /// <summary>
        /// Maximum records per segment (backup limit)
        /// </summary>
        public int MaxRecordsPerSegment { get; set; } = 100000;

        /// <summary>
        /// Enable compression for segments
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Auto-cleanup segments older than specified days
        /// </summary>
        public int AutoCleanupDays { get; set; } = 365;

        /// <summary>
        /// Enable real-time indexing
        /// </summary>
        public bool EnableRealTimeIndexing { get; set; } = true;

        // Auto-determined paths
        /// <summary>
        /// Data models directory path
        /// </summary>
        public string DataModelsPath => Path.Combine(BasePath, "datamodels");

        /// <summary>
        /// Temporary files directory path
        /// </summary>
        public string TempsPath => Path.Combine(BasePath, "temps");

        /// <summary>
        /// Backups directory path
        /// </summary>
        public string BackupsPath => Path.Combine(BasePath, "backups");

        /// <summary>
        /// Logs directory path
        /// </summary>
        public string LogsPath => Path.Combine(BasePath, "logs");

        /// <summary>
        /// Auto-determined file extension based on encryption
        /// </summary>
        public string FileExtension => EncryptionEnabled ? ".sde" : ".json";

        /// <summary>
        /// Validates configuration settings
        /// </summary>
        /// <returns>Validation result</returns>
        public ConfigValidationResult Validate()
        {
            var result = new ConfigValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(BasePath))
            {
                result.IsValid = false;
                result.Errors.Add("BasePath cannot be null or empty");
            }

            if (MaxSegmentSizeMB <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("MaxSegmentSizeMB must be greater than 0");
            }

            if (MaxRecordsPerSegment <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("MaxRecordsPerSegment must be greater than 0");
            }

            if (AutoCleanupDays < 0)
            {
                result.IsValid = false;
                result.Errors.Add("AutoCleanupDays cannot be negative");
            }

            if (EncryptionEnabled && Encryption == null)
            {
                result.IsValid = false;
                result.Errors.Add("Encryption config is required when encryption is enabled");
            }

            return result;
        }

        /// <summary>
        /// Creates a copy of the configuration
        /// </summary>
        /// <returns>Cloned configuration</returns>
        public HierarchicalDatabaseConfig Clone()
        {
            return new HierarchicalDatabaseConfig
            {
                BasePath = BasePath,
                EncryptionEnabled = EncryptionEnabled,
                Encryption = new EncryptionConfig
                {
                    EnableEncryption = Encryption.EnableEncryption,
                    CustomPassword = Encryption.CustomPassword,
                    CompressBeforeEncrypt = Encryption.CompressBeforeEncrypt,
                    FileExtension = Encryption.FileExtension,
                    EncryptionType = Encryption.EncryptionType,
                    KeyDerivationIterations = Encryption.KeyDerivationIterations,
                    IncludeIntegrityCheck = Encryption.IncludeIntegrityCheck
                },
                MaxSegmentSizeMB = MaxSegmentSizeMB,
                MaxRecordsPerSegment = MaxRecordsPerSegment,
                EnableCompression = EnableCompression,
                AutoCleanupDays = AutoCleanupDays,
                EnableRealTimeIndexing = EnableRealTimeIndexing
            };
        }
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
    }
}