using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Health
{
    internal class DataIntegrityHealthCheck : IHealthCheck
    {
        public string Name => "DataIntegrity";
        public HealthCategory Category => HealthCategory.DataIntegrity;

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
                var dataFiles = Directory.GetFiles(dataDirectory, "*.json", SearchOption.AllDirectories);

                var corruptedFiles = new List<string>();
                var emptyFiles = new List<string>();
                var validFiles = 0;

                // Check a sample of files (max 50 for performance)
                var filesToCheck = dataFiles.Take(50).ToList();

                foreach (var file in filesToCheck)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);

                        if (string.IsNullOrWhiteSpace(content))
                        {
                            emptyFiles.Add(Path.GetFileName(file));
                            continue;
                        }

                        // Try to parse as JSON
                        System.Text.Json.JsonDocument.Parse(content);
                        validFiles++;
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        corruptedFiles.Add(Path.GetFileName(file));
                    }
                    catch (Exception)
                    {
                        corruptedFiles.Add(Path.GetFileName(file));
                    }
                }

                result.Data["TotalFilesChecked"] = filesToCheck.Count;
                result.Data["ValidFiles"] = validFiles;
                result.Data["CorruptedFiles"] = corruptedFiles.Count;
                result.Data["EmptyFiles"] = emptyFiles.Count;
                result.Data["CorruptedFileNames"] = corruptedFiles.Take(10).ToList();
                result.Data["EmptyFileNames"] = emptyFiles.Take(10).ToList();

                if (corruptedFiles.Count > 0)
                {
                    result.Status = HealthStatus.Critical;
                    result.Message = $"Data integrity issues: {corruptedFiles.Count} corrupted files";
                    result.Recommendation = "Restore corrupted files from backup and investigate the cause";
                }
                else if (emptyFiles.Count > 0)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"Found {emptyFiles.Count} empty data files";
                    result.Recommendation = "Investigate why files are empty and restore if needed";
                }
                else
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = $"Data integrity is good ({validFiles} files checked)";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Warning;
                result.Message = $"Could not check data integrity: {ex.Message}";
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