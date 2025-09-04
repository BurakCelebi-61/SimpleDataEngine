namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance metric data
    /// </summary>
    public class PerformanceMetric
    {
        public string Operation { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public long MemoryUsed { get; set; }
        public string Category { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ThreadId { get; set; }
    }
}