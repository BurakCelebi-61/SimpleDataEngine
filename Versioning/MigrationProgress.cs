namespace SimpleDataEngine.Versioning
{
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
}