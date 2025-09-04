using SimpleDataEngine.Performance;

namespace SimpleDataEngine.Health
{
    internal class PerformanceHealthCheck : IHealthCheck
    {
        public string Name => "Performance";
        public HealthCategory Category => HealthCategory.Performance;

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
                // Now this works because PerformanceTracker.GetCurrentReport() is static
                var performanceReport = PerformanceTracker.GetCurrentReport();

                result.Data["AverageOperationTime"] = performanceReport.AverageOperationTimeMs;
                result.Data["SlowOperationsCount"] = performanceReport.SlowOperations.Count;
                result.Data["TotalOperations"] = performanceReport.TotalOperations;
                result.Data["OperationsPerSecond"] = performanceReport.OperationsPerSecond;

                // Check average operation time
                if (performanceReport.AverageOperationTimeMs > 1000) // More than 1 second
                {
                    result.Status = HealthStatus.Unhealthy;
                    result.Message = $"Slow average operation time: {performanceReport.AverageOperationTimeMs:F0}ms";
                    result.Recommendation = "Investigate performance bottlenecks and optimize slow operations";
                }
                else if (performanceReport.AverageOperationTimeMs > 500) // More than 500ms
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"Elevated average operation time: {performanceReport.AverageOperationTimeMs:F0}ms";
                    result.Recommendation = "Monitor performance and consider optimization";
                }
                else
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = $"Good performance: {performanceReport.AverageOperationTimeMs:F0}ms average operation time";
                }

                // Check for slow operations
                if (performanceReport.SlowOperations.Count > 10)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message += $" ({performanceReport.SlowOperations.Count} slow operations detected)";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"Could not check performance metrics: {ex.Message}";
                result.Exception = ex;
                return result;
            }
            finally
            {
                result.Duration = DateTime.Now - startTime;
            }
        }
    }
}