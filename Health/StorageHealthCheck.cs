using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Health
{
    internal class StorageHealthCheck : IHealthCheck
    {
        public string Name => "Storage";
        public HealthCategory Category => HealthCategory.Storage;

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

                // Check if data directory exists
                if (!Directory.Exists(dataDirectory))
                {
                    result.Status = HealthStatus.Critical;
                    result.Message = "Data directory does not exist";
                    result.Recommendation = $"Create data directory: {dataDirectory}";
                    return result;
                }

                // Check directory permissions
                var testFile = Path.Combine(dataDirectory, $"healthcheck_{Guid.NewGuid()}.tmp");
                try
                {
                    await File.WriteAllTextAsync(testFile, "health check");
                    File.Delete(testFile);
                }
                catch
                {
                    result.Status = HealthStatus.Critical;
                    result.Message = "Cannot write to data directory";
                    result.Recommendation = "Check directory permissions";
                    return result;
                }

                // Check storage files
                var files = Directory.GetFiles(dataDirectory, "*.json", SearchOption.AllDirectories);
                result.Data["FileCount"] = files.Length;
                result.Data["DataDirectory"] = dataDirectory;

                // Check for corrupted files
                int corruptedFiles = 0;
                foreach (var file in files.Take(10)) // Check first 10 files for performance
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        if (string.IsNullOrWhiteSpace(content))
                            corruptedFiles++;
                    }
                    catch
                    {
                        corruptedFiles++;
                    }
                }

                result.Data["CorruptedFiles"] = corruptedFiles;

                if (corruptedFiles > 0)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"Found {corruptedFiles} potentially corrupted files";
                    result.Recommendation = "Run data integrity check and restore from backup if needed";
                }
                else
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = "Storage is healthy";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Critical;
                result.Message = $"Storage check failed: {ex.Message}";
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