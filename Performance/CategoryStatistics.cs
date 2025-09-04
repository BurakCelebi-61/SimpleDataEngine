namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Category-based performance statistics
    /// </summary>
    public class CategoryStatistics
    {
        public string Category { get; set; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
        public long TotalMemoryUsed { get; set; }
        public List<string> TopOperations { get; set; } = new();
    }
}