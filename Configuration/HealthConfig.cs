namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Health check configuration settings
    /// </summary>
    public class HealthConfig
    {
        public bool EnableHealthChecks { get; set; } = true;
        public int HealthCheckIntervalMinutes { get; set; } = 5;
        public int HealthCheckTimeoutSeconds { get; set; } = 30;
        public bool NotifyOnHealthIssues { get; set; } = true;
    }
}