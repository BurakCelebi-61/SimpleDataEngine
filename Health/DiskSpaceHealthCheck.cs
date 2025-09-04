using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Health
{
    internal class DiskSpaceHealthCheck : IHealthCheck
    {
        public string Name => "DiskSpace";
        public HealthCategory Category => HealthCategory.DiskSpace;

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
                var dataDirectory = config.Database.DataDirectory;
                var driveInfo = new DriveInfo(Path.GetPathRoot(dataDirectory));

                var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var usedSpaceGB = totalSpaceGB - freeSpaceGB;
                var freeSpacePercentage = freeSpaceGB / totalSpaceGB * 100;

                result.Data["FreeSpaceGB"] = Math.Round(freeSpaceGB, 2);
                result.Data["TotalSpaceGB"] = Math.Round(totalSpaceGB, 2);
                result.Data["UsedSpaceGB"] = Math.Round(usedSpaceGB, 2);
                result.Data["FreeSpacePercentage"] = Math.Round(freeSpacePercentage, 1);
                result.Data["DriveName"] = driveInfo.Name;

                if (freeSpacePercentage < 5)
                {
                    result.Status = HealthStatus.Critical;
                    result.Message = $"Critically low disk space: {freeSpacePercentage:F1}% free ({freeSpaceGB:F1} GB)";
                    result.Recommendation = "Free up disk space immediately or add more storage";
                }
                else if (freeSpacePercentage < 10)
                {
                    result.Status = HealthStatus.Unhealthy;
                    result.Message = $"Low disk space: {freeSpacePercentage:F1}% free ({freeSpaceGB:F1} GB)";
                    result.Recommendation = "Consider freeing up disk space or adding more storage";
                }
                else if (freeSpacePercentage < 20)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"Disk space getting low: {freeSpacePercentage:F1}% free ({freeSpaceGB:F1} GB)";
                    result.Recommendation = "Monitor disk space usage";
                }
                else
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = $"Sufficient disk space: {freeSpacePercentage:F1}% free ({freeSpaceGB:F1} GB)";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"Could not check disk space: {ex.Message}";
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