namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Statistics for a specific operation type
    /// </summary>
    public class OperationStatistics
    {
        /// <summary>
        /// Name of the operation
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Total number of executions
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Number of successful executions
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// Number of failed executions
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// Average execution time in milliseconds
        /// </summary>
        public double AverageTimeMs { get; set; }

        /// <summary>
        /// Minimum execution time in milliseconds
        /// </summary>
        public double MinTimeMs { get; set; }

        /// <summary>
        /// Maximum execution time in milliseconds
        /// </summary>
        public double MaxTimeMs { get; set; }

        /// <summary>
        /// Total execution time for all operations
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// Success rate as percentage
        /// </summary>
        public double SuccessRate => ExecutionCount > 0 ? (double)SuccessfulExecutions / ExecutionCount * 100 : 0;

        /// <summary>
        /// Failure rate as percentage
        /// </summary>
        public double FailureRate => ExecutionCount > 0 ? (double)FailedExecutions / ExecutionCount * 100 : 0;

        /// <summary>
        /// Average memory consumption in bytes
        /// </summary>
        public long AverageMemoryUsage { get; set; }

        /// <summary>
        /// Peak memory usage in bytes
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// Operations per second (throughput)
        /// </summary>
        public double OperationsPerSecond { get; set; }

        /// <summary>
        /// First execution timestamp
        /// </summary>
        public DateTime FirstExecution { get; set; }

        /// <summary>
        /// Last execution timestamp
        /// </summary>
        public DateTime LastExecution { get; set; }

        /// <summary>
        /// Most recent error message
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Performance trend (improving/stable/degrading)
        /// </summary>
        public string PerformanceTrend { get; set; } = "Stable";

        /// <summary>
        /// Updates statistics with a new performance metric
        /// </summary>
        public void UpdateWith(PerformanceMetric metric)
        {
            if (metric.OperationName != OperationName && !string.IsNullOrEmpty(OperationName))
                return;

            OperationName = metric.OperationName;
            ExecutionCount++;

            if (metric.Success)
                SuccessfulExecutions++;
            else
            {
                FailedExecutions++;
                LastError = metric.ErrorMessage;
            }

            // Update timing statistics
            var durationMs = metric.DurationMs;

            if (ExecutionCount == 1)
            {
                MinTimeMs = MaxTimeMs = AverageTimeMs = durationMs;
                FirstExecution = metric.StartTime;
            }
            else
            {
                MinTimeMs = Math.Min(MinTimeMs, durationMs);
                MaxTimeMs = Math.Max(MaxTimeMs, durationMs);
                AverageTimeMs = (AverageTimeMs * (ExecutionCount - 1) + durationMs) / ExecutionCount;
            }

            TotalExecutionTime = TotalExecutionTime.Add(metric.Duration);
            LastExecution = metric.EndTime;

            // Update memory statistics
            if (metric.MemoryConsumed > 0)
            {
                AverageMemoryUsage = (AverageMemoryUsage * (ExecutionCount - 1) + metric.MemoryConsumed) / ExecutionCount;
                PeakMemoryUsage = Math.Max(PeakMemoryUsage, metric.MemoryConsumed);
            }

            // Calculate throughput
            var totalTimeSpan = LastExecution - FirstExecution;
            if (totalTimeSpan.TotalSeconds > 0)
            {
                OperationsPerSecond = ExecutionCount / totalTimeSpan.TotalSeconds;
            }
        }
    }
}