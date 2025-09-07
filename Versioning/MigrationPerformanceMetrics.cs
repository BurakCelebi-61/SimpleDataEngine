namespace SimpleDataEngine.Versioning
{
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
}