namespace SimpleDataEngine.Backup
{
    /// <summary>
    /// Predefined backup option sets
    /// </summary>
    public static class BackupPresets
    {
        /// <summary>
        /// Conservative backup settings - maximum safety, more storage usage
        /// </summary>
        public static BackupOptions Conservative => new()
        {
            Enabled = true,
            CompressBackups = true,
            CompressionLevel = BackupCompressionLevel.SmallestSize,
            MaxBackups = 50,
            MaxBackupAgeDays = 90,
            MaxBackupDirectorySizeMB = 5000,
            AutoBackupEnabled = true,
            AutoBackupInterval = BackupInterval.Hourly,
            RetentionPolicy = BackupRetentionPolicy.Smart,
            CreateSafetyBackups = true,
            ValidateAfterCreation = true,
            IncludePerformanceData = true,
            IncludeAuditLogs = true,
            IncludeConfiguration = true,
            NotifyOnCompletion = true,
            NotifyOnFailure = true,
            PauseDuringHeavyOperations = true,
            MinimumFreeDiskSpaceMB = 500
        };

        /// <summary>
        /// Balanced backup settings - good safety vs storage usage balance
        /// </summary>
        public static BackupOptions Balanced => new()
        {
            Enabled = true,
            CompressBackups = true,
            CompressionLevel = BackupCompressionLevel.Optimal,
            MaxBackups = 10,
            MaxBackupAgeDays = 30,
            MaxBackupDirectorySizeMB = 1000,
            AutoBackupEnabled = true,
            AutoBackupInterval = BackupInterval.Daily,
            RetentionPolicy = BackupRetentionPolicy.Smart,
            CreateSafetyBackups = true,
            ValidateAfterCreation = true,
            IncludePerformanceData = false,
            IncludeAuditLogs = true,
            IncludeConfiguration = true,
            NotifyOnCompletion = false,
            NotifyOnFailure = true,
            PauseDuringHeavyOperations = true,
            MinimumFreeDiskSpaceMB = 100
        };

        /// <summary>
        /// Minimal backup settings - least storage usage, basic safety
        /// </summary>
        public static BackupOptions Minimal => new()
        {
            Enabled = true,
            CompressBackups = true,
            CompressionLevel = BackupCompressionLevel.SmallestSize,
            MaxBackups = 3,
            MaxBackupAgeDays = 7,
            MaxBackupDirectorySizeMB = 100,
            AutoBackupEnabled = true,
            AutoBackupInterval = BackupInterval.Weekly,
            RetentionPolicy = BackupRetentionPolicy.KeepLast,
            CreateSafetyBackups = false,
            ValidateAfterCreation = false,
            IncludePerformanceData = false,
            IncludeAuditLogs = false,
            IncludeConfiguration = true,
            NotifyOnCompletion = false,
            NotifyOnFailure = true,
            PauseDuringHeavyOperations = false,
            MinimumFreeDiskSpaceMB = 50
        };

        /// <summary>
        /// Development backup settings - frequent backups for active development
        /// </summary>
        public static BackupOptions Development => new()
        {
            Enabled = true,
            CompressBackups = false, // Faster backup/restore for development
            CompressionLevel = BackupCompressionLevel.Fastest,
            MaxBackups = 20,
            MaxBackupAgeDays = 14,
            MaxBackupDirectorySizeMB = 2000,
            AutoBackupEnabled = true,
            AutoBackupInterval = BackupInterval.Hourly,
            RetentionPolicy = BackupRetentionPolicy.KeepLast,
            CreateSafetyBackups = true,
            ValidateAfterCreation = false, // Skip validation for speed
            IncludePerformanceData = true,
            IncludeAuditLogs = true,
            IncludeConfiguration = true,
            NotifyOnCompletion = false,
            NotifyOnFailure = true,
            PauseDuringHeavyOperations = false,
            MinimumFreeDiskSpaceMB = 200
        };
    }
}