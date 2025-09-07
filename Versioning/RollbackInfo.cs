namespace SimpleDataEngine.Versioning
{
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