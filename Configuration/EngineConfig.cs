namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Main engine configuration
    /// </summary>
    public class EngineConfig
    {
        public DatabaseConfig Database { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
        public BackupConfig Backup { get; set; } = new();
        public PerformanceConfig Performance { get; set; } = new();
        public HealthConfig Health { get; set; } = new();
        public ValidationConfig Validation { get; set; } = new();
        public string Version { get; set; } = "1.0.0";
        public string Environment { get; set; } = "Development";
        public bool DebugMode { get; set; } = false;

        /// <summary>
        /// Validates the engine configuration
        /// </summary>
        /// <returns>Validation result</returns>
        public ConfigValidationResult Validate()
        {
            var result = new ConfigValidationResult { IsValid = true };

            try
            {
                // Validate Database config
                if (string.IsNullOrWhiteSpace(Database?.ConnectionString))
                {
                    result.IsValid = false;
                    result.Errors.Add("Database.ConnectionString cannot be null or empty");
                }

                if (Database?.AutoBackupIntervalHours <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Database.AutoBackupIntervalHours must be greater than 0");
                }

                if (Database?.MaxDatabaseSizeMB <= 0)
                {
                    result.Warnings.Add("Database.MaxDatabaseSizeMB should be greater than 0");
                }

                // Validate Performance config
                if (Performance?.CacheExpirationMinutes <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Performance.CacheExpirationMinutes must be greater than 0");
                }

                if (Performance?.SlowQueryThresholdMs <= 0)
                {
                    result.Warnings.Add("Performance.SlowQueryThresholdMs should be greater than 0");
                }

                // Validate Backup config
                if (Backup?.RetentionDays <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Backup.RetentionDays must be greater than 0");
                }

                if (Backup?.MaxBackups < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Backup.MaxBackups cannot be negative");
                }

                // Validate Logging config
                if (Logging?.MaxLogFileSizeMB <= 0)
                {
                    result.Warnings.Add("Logging.MaxLogFileSizeMB should be greater than 0");
                }

                if (Logging?.MaxLogFiles <= 0)
                {
                    result.Warnings.Add("Logging.MaxLogFiles should be greater than 0");
                }

                // Validate directories exist or can be created
                try
                {
                    if (!string.IsNullOrWhiteSpace(Database?.DataDirectory))
                    {
                        var dataDir = Path.GetFullPath(Database.DataDirectory);
                        if (!Directory.Exists(dataDir))
                        {
                            result.Warnings.Add($"Data directory does not exist: {Database.DataDirectory}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Invalid data directory path: {ex.Message}");
                    result.IsValid = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Configuration validation failed: {ex.Message}");
                return result;
            }
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