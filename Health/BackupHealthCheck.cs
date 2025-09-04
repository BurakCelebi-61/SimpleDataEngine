using SimpleDataEngine.Backup;
using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Health
{
    internal class BackupHealthCheck : IHealthCheck
    {
        public string Name => "Backup";
        public HealthCategory Category => HealthCategory.Backup;

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
                var backups = BackupManager.GetAvailableBackups();
                var config = ConfigManager.Current.Backup;

                result.Data["BackupCount"] = backups.Count;
                result.Data["BackupsEnabled"] = config.Enabled;

                if (backups.Any())
                {
                    var latestBackup = backups.First(); // Already ordered by creation time
                    var backupAge = latestBackup.Age;

                    result.Data["LatestBackupAge"] = backupAge;
                    result.Data["LatestBackupSize"] = latestBackup.FileSizeFormatted;
                    result.Data["LatestBackupPath"] = latestBackup.FilePath;

                    // Check backup recency
                    if (config.Enabled)
                    {
                        var maxAgeHours = config.AutoBackupInterval switch
                        {
                            BackupInterval.Hourly => 2,
                            BackupInterval.Daily => 26, // 1 day + 2 hours buffer
                            BackupInterval.Weekly => 168 + 12, // 1 week + 12 hours buffer
                            BackupInterval.Monthly => 744 + 24, // 1 month + 1 day buffer
                            _ => 48 // Default 2 days
                        };

                        if (backupAge.TotalHours > maxAgeHours)
                        {
                            result.Status = HealthStatus.Warning;
                            result.Message = $"Latest backup is {backupAge.Days} days old";
                            result.Recommendation = "Consider running a manual backup or check automatic backup configuration";
                        }
                        else
                        {
                            result.Status = HealthStatus.Healthy;
                            result.Message = $"Latest backup is {(backupAge.TotalHours < 1 ? $"{backupAge.TotalMinutes:F0} minutes" : $"{backupAge.TotalHours:F1} hours")} old";
                        }
                    }
                    else
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = "Backups are disabled";
                        result.Recommendation = "Enable automatic backups for data protection";
                    }

                    // Validate a recent backup
                    var validation = BackupManager.ValidateBackup(latestBackup.FilePath);
                    result.Data["LatestBackupValid"] = validation.IsValid;

                    if (!validation.IsValid)
                    {
                        result.Status = HealthStatus.Unhealthy;
                        result.Message = $"Latest backup is corrupted: {string.Join(", ", validation.Errors)}";
                        result.Recommendation = "Create a new backup immediately";
                    }
                }
                else
                {
                    result.Status = config.Enabled ? HealthStatus.Critical : HealthStatus.Warning;
                    result.Message = "No backups found";
                    result.Recommendation = "Create a backup to ensure data protection";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"Could not check backup status: {ex.Message}";
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