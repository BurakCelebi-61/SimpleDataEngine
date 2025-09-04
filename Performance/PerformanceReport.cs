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
        public int FailedOperationsCount { get; set; } // Renamed from FailedOperations
        public double OverallSuccessRate { get; set; }
        public double AverageOperationTimeMs { get; set; } // Renamed from AverageResponseTime
        public double OperationsPerSecond { get; set; }

        public Dictionary<string, OperationStatistics> OperationStats { get; set; } = new();
        public Dictionary<string, CategoryStatistics> CategoryStats { get; set; } = new();
        public List<PerformanceMetric> SlowOperations { get; set; } = new();
        public List<PerformanceMetric> FailedOperations { get; set; } = new(); // This stays as the list
        public ResourceStatistics Resources { get; set; } = new();

        public List<PerformanceAlert> Alerts { get; set; } = new();
    }
}