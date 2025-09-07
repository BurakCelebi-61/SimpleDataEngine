namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Comprehensive performance report
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ReportPeriod { get; set; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double OverallSuccessRate { get; set; }
        public double AverageOperationTimeMs { get; set; }
        public double OperationsPerSecond { get; set; }

        public Dictionary<string, OperationStatistics> OperationStats { get; set; } = new();
        public Dictionary<string, CategoryStatistics> CategoryStats { get; set; } = new();
        public List<PerformanceMetric> SlowOperations { get; set; } = new();
        public List<PerformanceMetric> FailedOperationsList { get; set; } = new();
        public ResourceStatistics Resources { get; set; } = new();

        public List<PerformanceAlert> Alerts { get; set; } = new();

        public override string ToString()
        {
            return $"PerformanceReport[Operations: {TotalOperations}, Success Rate: {OverallSuccessRate:F1}%, Avg Time: {AverageOperationTimeMs:F2}ms]";
        }
    }





}