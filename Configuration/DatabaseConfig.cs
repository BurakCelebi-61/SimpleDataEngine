namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Database configuration settings
    /// </summary>
    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = "Database";
        public string DataDirectory { get; set; } = "database";
        public string BackupDirectory { get; set; } = "backups";
        public int AutoBackupIntervalHours { get; set; } = 24;
        public bool EnableAutoBackup { get; set; } = true;
        public int MaxDatabaseSizeMB { get; set; } = 1000;
    }
}