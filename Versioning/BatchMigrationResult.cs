namespace SimpleDataEngine.Versioning
{
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
}