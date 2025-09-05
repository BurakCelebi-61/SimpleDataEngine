using System.ComponentModel;

namespace SimpleDataEngine.Backup
{

    /// <summary>
    /// Configuration options for backup operations
    /// </summary>
    public class BackupOptions
    {
        /// <summary>
        /// Whether backups are enabled
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether to compress backups
        /// </summary>
        [DefaultValue(true)]
        public bool CompressBackups { get; set; } = true;

        /// <summary>
        /// Compression level for backups
        /// </summary>
        [DefaultValue(BackupCompressionLevel.Optimal)]
        public BackupCompressionLevel CompressionLevel { get; set; } = BackupCompressionLevel.Optimal;

        /// <summary>
        /// Maximum number of backups to keep (0 = unlimited)
        /// </summary>
        [DefaultValue(10)]
        public int MaxBackups { get; set; } = 10;

        /// <summary>
        /// Maximum age of backups in days (0 = no age limit)
        /// </summary>
        [DefaultValue(30)]
        public int MaxBackupAgeDays { get; set; } = 30;

        /// <summary>
        /// Maximum total size of backup directory in MB (0 = no size limit)
        /// </summary>
        [DefaultValue(1000)]
        public long MaxBackupDirectorySizeMB { get; set; } = 1000;

        /// <summary>
        /// Backup filename format (DateTime.ToString format)
        /// </summary>
        [DefaultValue("SimpleDataEngine_Backup_{0:yyyyMMdd_HHmmss}")]
        public string BackupNameFormat { get; set; } = "SimpleDataEngine_Backup_{0:yyyyMMdd_HHmmss}";

        /// <summary>
        /// Whether to create automatic backups
        /// </summary>
        [DefaultValue(true)]
        public bool AutoBackupEnabled { get; set; } = true;

        /// <summary>
        /// Automatic backup interval
        /// </summary>
        [DefaultValue(BackupInterval.Daily)]
        public BackupInterval AutoBackupInterval { get; set; } = BackupInterval.Daily;

        /// <summary>
        /// Time of day for automatic backups (for daily/weekly/monthly intervals)
        /// </summary>
        [DefaultValue(typeof(TimeSpan), "02:00:00")]
        public TimeSpan AutoBackupTime { get; set; } = TimeSpan.FromHours(2);

        /// <summary>
        /// Day of week for weekly backups (0 = Sunday)
        /// </summary>
        [DefaultValue(DayOfWeek.Sunday)]
        public DayOfWeek AutoBackupDayOfWeek { get; set; } = DayOfWeek.Sunday;

        /// <summary>
        /// Day of month for monthly backups (1-28)
        /// </summary>
        [DefaultValue(1)]
        public int AutoBackupDayOfMonth { get; set; } = 1;

        /// <summary>
        /// Backup retention policy
        /// </summary>
        [DefaultValue(BackupRetentionPolicy.Smart)]
        public BackupRetentionPolicy RetentionPolicy { get; set; } = BackupRetentionPolicy.Smart;

        /// <summary>
        /// Whether to create backup before major operations (restore, bulk delete, etc.)
        /// </summary>
        [DefaultValue(true)]
        public bool CreateSafetyBackups { get; set; } = true;

        /// <summary>
        /// Whether to validate backups after creation
        /// </summary>
        [DefaultValue(true)]
        public bool ValidateAfterCreation { get; set; } = true;

        /// <summary>
        /// Whether to include performance data in backups
        /// </summary>
        [DefaultValue(false)]
        public bool IncludePerformanceData { get; set; } = false;

        /// <summary>
        /// Whether to include audit logs in backups
        /// </summary>
        [DefaultValue(true)]
        public bool IncludeAuditLogs { get; set; } = true;

        /// <summary>
        /// Whether to include configuration files in backups
        /// </summary>
        [DefaultValue(true)]
        public bool IncludeConfiguration { get; set; } = true;

        /// <summary>
        /// Custom backup directory path (null = use default)
        /// </summary>
        public string CustomBackupDirectory { get; set; }

        /// <summary>
        /// Whether to notify on backup completion
        /// </summary>
        [DefaultValue(true)]
        public bool NotifyOnCompletion { get; set; } = true;

        /// <summary>
        /// Whether to notify on backup failures
        /// </summary>
        [DefaultValue(true)]
        public bool NotifyOnFailure { get; set; } = true;

        /// <summary>
        /// Custom backup description template
        /// </summary>
        public string DescriptionTemplate { get; set; } = "Auto backup - {0:yyyy-MM-dd HH:mm:ss}";

        /// <summary>
        /// Whether to pause backups during heavy operations
        /// </summary>
        [DefaultValue(true)]
        public bool PauseDuringHeavyOperations { get; set; } = true;

        /// <summary>
        /// Minimum free disk space required for backup (in MB)
        /// </summary>
        [DefaultValue(100)]
        public long MinimumFreeDiskSpaceMB { get; set; } = 100;

        /// <summary>
        /// Gets the effective backup directory path
        /// </summary>
        public string GetBackupDirectory()
        {
            return !string.IsNullOrWhiteSpace(CustomBackupDirectory)
                ? CustomBackupDirectory
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                              "SimpleDataEngine", "Backups");
        }

        /// <summary>
        /// Validates the backup options
        /// </summary>
        /// <returns>Validation result with any issues</returns>
        public BackupOptionsValidation Validate()
        {
            var validation = new BackupOptionsValidation { IsValid = true };

            // Validate max backups
            if (MaxBackups < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxBackups cannot be negative");
            }

            // Validate max age
            if (MaxBackupAgeDays < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxBackupAgeDays cannot be negative");
            }

            // Validate max directory size
            if (MaxBackupDirectorySizeMB < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxBackupDirectorySizeMB cannot be negative");
            }

            // Validate backup name format
            if (string.IsNullOrWhiteSpace(BackupNameFormat))
            {
                validation.IsValid = false;
                validation.Errors.Add("BackupNameFormat cannot be empty");
            }
            else
            {
                try
                {
                    string.Format(BackupNameFormat, DateTime.Now);
                }
                catch
                {
                    validation.IsValid = false;
                    validation.Errors.Add("BackupNameFormat contains invalid format string");
                }
            }

            // Validate auto backup time
            if (AutoBackupTime < TimeSpan.Zero || AutoBackupTime >= TimeSpan.FromDays(1))
            {
                validation.IsValid = false;
                validation.Errors.Add("AutoBackupTime must be between 00:00:00 and 23:59:59");
            }

            // Validate day of month
            if (AutoBackupDayOfMonth < 1 || AutoBackupDayOfMonth > 28)
            {
                validation.IsValid = false;
                validation.Errors.Add("AutoBackupDayOfMonth must be between 1 and 28");
            }

            // Validate minimum free disk space
            if (MinimumFreeDiskSpaceMB < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MinimumFreeDiskSpaceMB cannot be negative");
            }

            // Validate custom backup directory if specified
            if (!string.IsNullOrWhiteSpace(CustomBackupDirectory))
            {
                try
                {
                    var fullPath = Path.GetFullPath(CustomBackupDirectory);
                    if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                    {
                        validation.Warnings.Add($"Custom backup directory parent does not exist: {CustomBackupDirectory}");
                    }
                }
                catch
                {
                    validation.IsValid = false;
                    validation.Errors.Add("CustomBackupDirectory contains invalid path characters");
                }
            }

            // Check for conflicting retention settings
            if (RetentionPolicy == BackupRetentionPolicy.KeepLast && MaxBackups == 0)
            {
                validation.Warnings.Add("KeepLast retention policy with MaxBackups=0 will keep unlimited backups");
            }

            if (RetentionPolicy == BackupRetentionPolicy.KeepByAge && MaxBackupAgeDays == 0)
            {
                validation.Warnings.Add("KeepByAge retention policy with MaxBackupAgeDays=0 will keep all backups");
            }

            if (RetentionPolicy == BackupRetentionPolicy.KeepBySize && MaxBackupDirectorySizeMB == 0)
            {
                validation.Warnings.Add("KeepBySize retention policy with MaxBackupDirectorySizeMB=0 has no size limit");
            }

            return validation;
        }

        /// <summary>
        /// Creates a copy of the current options
        /// </summary>
        /// <returns>Deep copy of backup options</returns>
        public BackupOptions Clone()
        {
            return new BackupOptions
            {
                Enabled = Enabled,
                CompressBackups = CompressBackups,
                CompressionLevel = CompressionLevel,
                MaxBackups = MaxBackups,
                MaxBackupAgeDays = MaxBackupAgeDays,
                MaxBackupDirectorySizeMB = MaxBackupDirectorySizeMB,
                BackupNameFormat = BackupNameFormat,
                AutoBackupEnabled = AutoBackupEnabled,
                AutoBackupInterval = AutoBackupInterval,
                AutoBackupTime = AutoBackupTime,
                AutoBackupDayOfWeek = AutoBackupDayOfWeek,
                AutoBackupDayOfMonth = AutoBackupDayOfMonth,
                RetentionPolicy = RetentionPolicy,
                CreateSafetyBackups = CreateSafetyBackups,
                ValidateAfterCreation = ValidateAfterCreation,
                IncludePerformanceData = IncludePerformanceData,
                IncludeAuditLogs = IncludeAuditLogs,
                IncludeConfiguration = IncludeConfiguration,
                CustomBackupDirectory = CustomBackupDirectory,
                NotifyOnCompletion = NotifyOnCompletion,
                NotifyOnFailure = NotifyOnFailure,
                DescriptionTemplate = DescriptionTemplate,
                PauseDuringHeavyOperations = PauseDuringHeavyOperations,
                MinimumFreeDiskSpaceMB = MinimumFreeDiskSpaceMB
            };
        }
    }
}