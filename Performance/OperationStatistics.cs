namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Statistics for a specific operation
    /// </summary>
    public class OperationStatistics
    {
        public string Operation { get; set; }
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double MedianResponseTime { get; set; }
        public DateTime FirstCall { get; set; }
        public DateTime LastCall { get; set; }
    }
}