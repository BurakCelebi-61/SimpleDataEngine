namespace SimpleDataEngine.Versioning
{
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
}