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

    /// <summary>
    /// Migration statistics
    /// </summary>
    public class MigrationStatistics
    {
        /// <summary>
        /// Items added during migration
        /// </summary>
        public int ItemsAdded { get; set; }

        /// <summary>
        /// Items modified during migration
        /// </summary>
        public int ItemsModified { get; set; }

        /// <summary>
        /// Items removed during migration
        /// </summary>
        public int ItemsRemoved { get; set; }

        /// <summary>
        /// Items that failed to migrate
        /// </summary>
        public int ItemsFailed { get; set; }

        /// <summary>
        /// Properties added to entities
        /// </summary>
        public int PropertiesAdded { get; set; }

        /// <summary>
        /// Properties removed from entities
        /// </summary>
        public int PropertiesRemoved { get; set; }

        /// <summary>
        /// Properties renamed
        /// </summary>
        public int PropertiesRenamed { get; set; }

        /// <summary>
        /// Data transformations applied
        /// </summary>
        public int TransformationsApplied { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate
        {
            get
            {
                var total = ItemsAdded + ItemsModified + ItemsRemoved + ItemsFailed;
                return total > 0 ? ((total - ItemsFailed) / (double)total) * 100 : 100;
            }
        }
    }

    /// <summary>
    /// Migration performance metrics
    /// </summary>
    public class MigrationPerformanceMetrics
    {
        /// <summary>
        /// Items per second processing rate
        /// </summary>
        public double ItemsPerSecond { get; set; }

        /// <summary>
        /// Memory usage during migration (bytes)
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Peak memory usage during migration (bytes)
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// CPU time used during migration
        /// </summary>
        public TimeSpan CpuTime { get; set; }

        /// <summary>
        /// I/O operations performed
        /// </summary>
        public int IoOperations { get; set; }

        /// <summary>
        /// Migration efficiency score (0-100)
        /// </summary>
        public double EfficiencyScore { get; set; }
    }

    /// <summary>
    /// Batch migration result for multiple migrations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class BatchMigrationResult<T> where T : class
    {
        /// <summary>
        /// Whether all migrations were successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Starting version
        /// </summary>
        public DataVersion FromVersion { get; set; }

        /// <summary>
        /// Final version
        /// </summary>
        public DataVersion ToVersion { get; set; }

        /// <summary>
        /// Total migration time
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Individual migration results
        /// </summary>
        public List<MigrationResult<T>> MigrationResults { get; set; } = new();

        /// <summary>
        /// Final migrated data
        /// </summary>
        public List<T> FinalData { get; set; }

        /// <summary>
        /// Number of successful migrations
        /// </summary>
        public int SuccessfulMigrations => MigrationResults.Count(r => r.Success);

        /// <summary>
        /// Number of failed migrations
        /// </summary>
        public int FailedMigrations => MigrationResults.Count(r => !r.Success);

        /// <summary>
        /// Total items processed across all migrations
        /// </summary>
        public int TotalItemsProcessed => MigrationResults.Sum(r => r.ItemsProcessed);

        /// <summary>
        /// All backup paths created during migration
        /// </summary>
        public List<string> BackupPaths => MigrationResults
            .Where(r => !string.IsNullOrEmpty(r.BackupPath))
            .Select(r => r.BackupPath)
            .ToList();

        /// <summary>
        /// All warnings from all migrations
        /// </summary>
        public List<string> AllWarnings => MigrationResults
            .SelectMany(r => r.Warnings)
            .ToList();

        /// <summary>
        /// Migration path taken (list of versions)
        /// </summary>
        public List<DataVersion> MigrationPath { get; set; } = new();

        /// <summary>
        /// Overall migration statistics
        /// </summary>
        public MigrationStatistics OverallStatistics { get; set; } = new();

        /// <summary>
        /// Rollback information if migration failed
        /// </summary>
        public RollbackInfo RollbackInfo { get; set; }
    }

    /// <summary>
    /// Rollback information
    /// </summary>
    public class RollbackInfo
    {
        /// <summary>
        /// Whether rollback is possible
        /// </summary>
        public bool CanRollback { get; set; }

        /// <summary>
        /// Version to rollback to
        /// </summary>
        public DataVersion RollbackToVersion { get; set; }

        /// <summary>
        /// Available backup paths for rollback
        /// </summary>
        public List<string> AvailableBackups { get; set; } = new();

        /// <summary>
        /// Migrations that need to be reversed
        /// </summary>
        public List<string> MigrationsToReverse { get; set; } = new();

        /// <summary>
        /// Estimated rollback time
        /// </summary>
        public TimeSpan EstimatedRollbackTime { get; set; }

        /// <summary>
        /// Rollback risk level
        /// </summary>
        public MigrationRiskLevel RollbackRisk { get; set; } = MigrationRiskLevel.Low;
    }
}