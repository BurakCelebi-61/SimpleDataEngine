namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance alert information
    /// </summary>
    public class PerformanceAlert
    {
        public enum AlertLevel
        {
            Info,
            Warning,
            Critical
        }

        public AlertLevel Level { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public DateTime DetectedAt { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
}