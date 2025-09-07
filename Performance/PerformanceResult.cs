namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance result data
    /// </summary>
    public class PerformanceResult
    {
        public string OperationName { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public double DurationMs => Duration.TotalMilliseconds;

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public double DurationSeconds => Duration.TotalSeconds;

        /// <summary>
        /// Performance category based on duration
        /// </summary>
        public PerformanceCategory Category
        {
            get
            {
                return DurationMs switch
                {
                    < 10 => PerformanceCategory.Excellent,
                    < 100 => PerformanceCategory.Good,
                    < 1000 => PerformanceCategory.Acceptable,
                    < 5000 => PerformanceCategory.Slow,
                    _ => PerformanceCategory.VerySlow
                };
            }
        }

        public override string ToString()
        {
            return $"{OperationName}: {DurationMs:F2}ms ({Category})";
        }
    }
}