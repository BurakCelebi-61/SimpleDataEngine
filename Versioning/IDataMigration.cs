namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Interface for data migration operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IDataMigration<T> where T : class
    {
        /// <summary>
        /// Source version for this migration
        /// </summary>
        DataVersion FromVersion { get; }

        /// <summary>
        /// Target version for this migration
        /// </summary>
        DataVersion ToVersion { get; }

        /// <summary>
        /// Migration name/description
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Migration description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this migration is reversible
        /// </summary>
        bool IsReversible { get; }

        /// <summary>
        /// Executes the migration
        /// </summary>
        /// <param name="data">Data to migrate</param>
        /// <param name="context">Migration context</param>
        /// <returns>Migrated data</returns>
        Task<MigrationResult<T>> MigrateAsync(List<T> data, MigrationContext context);

        /// <summary>
        /// Reverses the migration (if supported)
        /// </summary>
        /// <param name="data">Data to reverse</param>
        /// <param name="context">Migration context</param>
        /// <returns>Reversed data</returns>
        Task<MigrationResult<T>> ReverseAsync(List<T> data, MigrationContext context);

        /// <summary>
        /// Validates that the migration can be applied
        /// </summary>
        /// <param name="data">Data to validate</param>
        /// <param name="context">Migration context</param>
        /// <returns>Validation result</returns>
        Task<MigrationValidationResult> ValidateAsync(List<T> data, MigrationContext context);

        /// <summary>
        /// Creates a backup before migration
        /// </summary>
        /// <param name="data">Data to backup</param>
        /// <param name="backupPath">Backup file path</param>
        /// <returns>True if backup was successful</returns>
        Task<bool> CreateBackupAsync(List<T> data, string backupPath);
    }

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

    /// <summary>
    /// Migration progress information
    /// </summary>
    public class MigrationProgress
    {
        /// <summary>
        /// Current migration step
        /// </summary>
        public string CurrentStep { get; set; }

        /// <summary>
        /// Items processed so far
        /// </summary>
        public int ProcessedItems { get; set; }

        /// <summary>
        /// Total items to process
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Migration progress percentage
        /// </summary>
        public double ProgressPercentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;

        /// <summary>
        /// Elapsed migration time
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Current migration operation
        /// </summary>
        public string CurrentOperation { get; set; }

        /// <summary>
        /// Any errors encountered during migration
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Any warnings encountered during migration
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Migration validation result
    /// </summary>
    public class MigrationValidationResult
    {
        /// <summary>
        /// Whether the migration is valid
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Estimated migration duration
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Number of items that will be affected
        /// </summary>
        public int AffectedItemCount { get; set; }

        /// <summary>
        /// Whether the migration will cause data loss
        /// </summary>
        public bool WillCauseDataLoss { get; set; }

        /// <summary>
        /// Migration risk level
        /// </summary>
        public MigrationRiskLevel RiskLevel { get; set; } = MigrationRiskLevel.Low;

        /// <summary>
        /// Additional validation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Migration risk levels
    /// </summary>
    public enum MigrationRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}