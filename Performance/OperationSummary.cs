namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Operation summary data
    /// </summary>
    public class OperationSummary
    {
        public string OperationName { get; set; }
        public int Count { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}