namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance metric data structure
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// Name of the operation being measured
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier for this metric
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the operation started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When the operation ended
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the operation
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public double DurationMs => Duration.TotalMilliseconds;

        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Category of the operation
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Memory usage before operation (in bytes)
        /// </summary>
        public long MemoryBefore { get; set; }

        /// <summary>
        /// Memory usage after operation (in bytes)
        /// </summary>
        public long MemoryAfter { get; set; }

        /// <summary>
        /// Memory consumed during operation
        /// </summary>
        public long MemoryConsumed => MemoryAfter - MemoryBefore;

        /// <summary>
        /// Thread ID where operation executed
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Additional metadata for the operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception details if operation failed
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Number of records processed (if applicable)
        /// </summary>
        public int? RecordCount { get; set; }

        /// <summary>
        /// Creates a completed performance metric
        /// </summary>
        public static PerformanceMetric CreateCompleted(string operationName, DateTime startTime, DateTime endTime, bool success = true)
        {
            return new PerformanceMetric
            {
                OperationName = operationName,
                StartTime = startTime,
                EndTime = endTime,
                Success = success,
                ThreadId = Environment.CurrentManagedThreadId
            };
        }
    }
}