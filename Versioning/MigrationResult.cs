namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Result of a migration operation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class MigrationResult<T> where T : class
    {
        /// <summary>
        /// Whether the migration was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Source version
        /// </summary>
        public DataVersion FromVersion { get; set; }

        /// <summary>
        /// Target version
        /// </summary>
        public DataVersion ToVersion { get; set; }

        /// <summary>
        /// Migration start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Migration end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Migration duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Original data before migration
        /// </summary>
        public List<T> OriginalData { get; set; }

        /// <summary>
        /// Migrated data
        /// </summary>
        public List<T> MigratedData { get; set; }

        /// <summary>
        /// Number of items processed
        /// </summary>
        public int ItemsProcessed { get; set; }

        /// <summary>
        /// Error message if migration failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Exception that occurred during migration
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Backup file path (if backup was created)
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Validation result
        /// </summary>
        public MigrationValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Migration warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Additional migration metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Migration statistics
        /// </summary>
        public MigrationStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Whether a backup was created
        /// </summary>
        public bool BackupCreated => !string.IsNullOrEmpty(BackupPath);

        /// <summary>
        /// Migration performance metrics
        /// </summary>
        public MigrationPerformanceMetrics Performance { get; set; } = new();
    }
}