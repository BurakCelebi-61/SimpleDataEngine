namespace SimpleDataEngine.Health
{
    /// <summary>
    /// Health check options
    /// </summary>
    public class HealthCheckOptions
    {
        /// <summary>
        /// Whether to include detailed system information
        /// </summary>
        public bool IncludeSystemInfo { get; set; } = true;

        /// <summary>
        /// Whether to run performance-intensive checks
        /// </summary>
        public bool IncludePerformanceChecks { get; set; } = true;

        /// <summary>
        /// Whether to validate data integrity
        /// </summary>
        public bool ValidateDataIntegrity { get; set; } = false;

        /// <summary>
        /// Timeout for individual health checks
        /// </summary>
        public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Categories to include in health check (null = all)
        /// </summary>
        public List<HealthCategory> IncludeCategories { get; set; }

        /// <summary>
        /// Categories to exclude from health check
        /// </summary>
        public List<HealthCategory> ExcludeCategories { get; set; } = new();
    }
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
}