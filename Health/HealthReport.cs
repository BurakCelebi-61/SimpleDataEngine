namespace SimpleDataEngine.Health
{
    /// <summary>
    /// Overall health report
    /// </summary>
    public class HealthReport
    {
        public HealthStatus OverallStatus { get; set; }
        public List<HealthCheckResult> Checks { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
        public Dictionary<string, object> SystemInfo { get; set; } = new();

        public bool IsHealthy => OverallStatus == HealthStatus.Healthy;
        public int HealthyCount => Checks.Count(c => c.Status == HealthStatus.Healthy);
        public int WarningCount => Checks.Count(c => c.Status == HealthStatus.Warning);
        public int UnhealthyCount => Checks.Count(c => c.Status == HealthStatus.Unhealthy);
        public int CriticalCount => Checks.Count(c => c.Status == HealthStatus.Critical);

        public List<HealthCheckResult> GetByCategory(HealthCategory category)
        {
            return Checks.Where(c => c.Category == category).ToList();
        }

        public List<HealthCheckResult> GetByStatus(HealthStatus status)
        {
            return Checks.Where(c => c.Status == status).ToList();
        }
    }
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
}