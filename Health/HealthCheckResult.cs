namespace SimpleDataEngine.Health
{

    /// <summary>
    /// Individual health check result
    /// </summary>
    public class HealthCheckResult
    {
        public string Name { get; set; }
        public HealthCategory Category { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public string Recommendation { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.Now;
        public Exception Exception { get; set; }

        public bool IsHealthy => Status == HealthStatus.Healthy;
        public bool IsWarning => Status == HealthStatus.Warning;
        public bool IsUnhealthy => Status == HealthStatus.Unhealthy || Status == HealthStatus.Critical;
    }
}