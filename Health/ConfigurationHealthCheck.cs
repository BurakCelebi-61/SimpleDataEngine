using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Health
{
    internal class ConfigurationHealthCheck : IHealthCheck
    {
        public string Name => "Configuration";
        public HealthCategory Category => HealthCategory.Configuration;

        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            var startTime = DateTime.Now;
            var result = new HealthCheckResult
            {
                Name = Name,
                Category = Category
            };

            try
            {
                var config = ConfigManager.Current;

                // Validate configuration
                var validation = config.Validate();

                result.Data["ConfigurationFile"] = ConfigManager.ConfigFilePath;
                result.Data["ValidationErrors"] = validation.Errors.Count;
                result.Data["ValidationWarnings"] = validation.Warnings.Count;

                if (!validation.IsValid)
                {
                    result.Status = HealthStatus.Critical;
                    result.Message = $"Configuration validation failed: {string.Join(", ", validation.Errors)}";
                    result.Recommendation = "Fix configuration errors and restart application";
                }
                else if (validation.Warnings.Any())
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"Configuration has warnings: {string.Join(", ", validation.Warnings)}";
                    result.Recommendation = "Review configuration warnings";
                }
                else
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = "Configuration is valid";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Critical;
                result.Message = $"Configuration check failed: {ex.Message}";
                result.Exception = ex;
                return result;
            }
            finally
            {
                result.Duration = DateTime.Now - startTime;
            }
        }
    }
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
}