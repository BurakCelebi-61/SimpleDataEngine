namespace SimpleDataEngine.Health
{
    internal class MemoryHealthCheck : IHealthCheck
    {
        public string Name => "Memory";
        public HealthCategory Category => HealthCategory.Memory;

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
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / (1024.0 * 1024.0);
                var privateMemoryMB = process.PrivateMemorySize64 / (1024.0 * 1024.0);

                // Force garbage collection for accurate measurement
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var managedMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                result.Data["WorkingSetMB"] = Math.Round(workingSetMB, 2);
                result.Data["PrivateMemoryMB"] = Math.Round(privateMemoryMB, 2);
                result.Data["ManagedMemoryMB"] = Math.Round(managedMemoryMB, 2);
                result.Data["Gen0Collections"] = GC.CollectionCount(0);
                result.Data["Gen1Collections"] = GC.CollectionCount(1);
                result.Data["Gen2Collections"] = GC.CollectionCount(2);

                if (workingSetMB > 1000) // More than 1GB
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"High memory usage: {workingSetMB:F0} MB";
                    result.Recommendation = "Monitor memory usage and consider optimization";
                }
                else if (workingSetMB > 2000) // More than 2GB
                {
                    result.Status = HealthStatus.Unhealthy;
                    result.Message = $"Very high memory usage: {workingSetMB:F0} MB";
                    result.Recommendation = "Investigate memory leaks or reduce memory usage";
                }
                else
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = $"Normal memory usage: {workingSetMB:F0} MB";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"Could not check memory usage: {ex.Message}";
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