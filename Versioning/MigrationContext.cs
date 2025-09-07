namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Migration context providing environment information
    /// </summary>
    public class MigrationContext
    {
        /// <summary>
        /// Current data version
        /// </summary>
        public DataVersion CurrentVersion { get; set; }

        /// <summary>
        /// Target data version
        /// </summary>
        public DataVersion TargetVersion { get; set; }

        /// <summary>
        /// Migration start time
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether to create backups during migration
        /// </summary>
        public bool CreateBackups { get; set; } = true;

        /// <summary>
        /// Backup directory path
        /// </summary>
        public string BackupDirectory { get; set; } = "./migration_backups";

        /// <summary>
        /// Whether to validate data after migration
        /// </summary>
        public bool ValidateAfterMigration { get; set; } = true;

        /// <summary>
        /// Maximum allowed migration time
        /// </summary>
        public TimeSpan MaxMigrationTime { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Progress callback for reporting migration progress
        /// </summary>
        public Action<MigrationProgress> ProgressCallback { get; set; }

        /// <summary>
        /// Custom migration properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Whether to stop on first error
        /// </summary>
        public bool StopOnFirstError { get; set; } = true;

        /// <summary>
        /// Whether to run in dry-run mode (validation only)
        /// </summary>
        public bool DryRun { get; set; } = false;
    }
}