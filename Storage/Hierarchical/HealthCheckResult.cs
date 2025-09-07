namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Health check result
    /// </summary>
    public class HealthCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public HealthCategory Category { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }
}