namespace SimpleDataEngine.Health
{
    /// <summary>
    /// Interface for custom health checks
    /// </summary>
    public interface IHealthCheck
    {
        string Name { get; }
        HealthCategory Category { get; }
        Task<HealthCheckResult> CheckHealthAsync();
    }
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
}