using SimpleDataEngine.Backup;

namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Backup configuration settings
    /// </summary>
    public class BackupConfig
    {
        public bool Enabled { get; set; } = true;
        public int MaxBackups { get; set; } = 10;
        public int MaxBackupCount { get; set; } = 10;
        public bool CompressBackups { get; set; } = true;
        public string BackupNameFormat { get; set; } = "backup_{0:yyyyMMdd_HHmmss}";
        public int RetentionDays { get; set; } = 30;
        public bool EnableAutoCleanup { get; set; } = true;
        public BackupInterval AutoBackupInterval { get; set; } = BackupInterval.Daily;
        public string BackupDirectory { get; set; } = "backups";
    }
}