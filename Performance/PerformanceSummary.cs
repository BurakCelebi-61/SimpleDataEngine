namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance summary data
    /// </summary>
    public class PerformanceSummary
    {
        public int TotalOperations { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan MedianDuration { get; set; }
        public TimeSpan P95Duration { get; set; }
        public TimeSpan P99Duration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan? TimeWindow { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<OperationSummary> OperationSummaries { get; set; } = new();
        public Dictionary<PerformanceCategory, CategoryStats> CategoryBreakdown { get; set; } = new();
    }
}