using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Health
{
    /// <summary>
    /// Health monitoring and diagnostics for SimpleDataEngine
    /// </summary>
    public static class HealthChecker
    {
        private static readonly List<IHealthCheck> _customChecks = new();
        private static readonly object _lock = new object();

        /// <summary>
        /// Performs a comprehensive health check
        /// </summary>
        /// <param name="options">Health check options</param>
        /// <returns>Health report</returns>
        public static async Task<HealthReport> CheckHealthAsync(HealthCheckOptions options = null)
        {
            options ??= new HealthCheckOptions();
            var startTime = DateTime.Now;
            var report = new HealthReport();

            try
            {
                // Collect system information
                if (options.IncludeSystemInfo)
                {
                    CollectSystemInfo(report);
                }

                // Run built-in health checks
                var builtInChecks = GetBuiltInHealthChecks(options);
                var customChecks = GetCustomHealthChecks(options);
                var allChecks = builtInChecks.Concat(customChecks);

                var checkTasks = allChecks.Select(async check =>
                {
                    try
                    {
                        var checkTask = check.CheckHealthAsync();
                        var timeoutTask = Task.Delay(options.CheckTimeout);

                        var completedTask = await Task.WhenAny(checkTask, timeoutTask);

                        if (completedTask == timeoutTask)
                        {
                            return new HealthCheckResult
                            {
                                Name = check.Name,
                                Category = check.Category,
                                Status = HealthStatus.Critical,
                                Message = $"Health check timed out after {options.CheckTimeout}",
                                Duration = options.CheckTimeout
                            };
                        }

                        return await checkTask;
                    }
                    catch (Exception ex)
                    {
                        return new HealthCheckResult
                        {
                            Name = check.Name,
                            Category = check.Category,
                            Status = HealthStatus.Critical,
                            Message = $"Health check failed: {ex.Message}",
                            Exception = ex,
                            Duration = DateTime.Now - startTime
                        };
                    }
                });

                var results = await Task.WhenAll(checkTasks);
                report.Checks.AddRange(results);

                // Determine overall status
                report.OverallStatus = DetermineOverallStatus(report.Checks);
                report.TotalDuration = DateTime.Now - startTime;

                // Log health check
                AuditLogger.Log("HEALTH_CHECK_COMPLETED", new
                {
                    report.OverallStatus,
                    TotalChecks = report.Checks.Count,
                    report.HealthyCount,
                    report.WarningCount,
                    report.UnhealthyCount,
                    report.CriticalCount,
                    Duration = report.TotalDuration
                });

                return report;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("HEALTH_CHECK_FAILED", ex);

                report.OverallStatus = HealthStatus.Critical;
                report.Checks.Add(new HealthCheckResult
                {
                    Name = "HealthChecker",
                    Category = HealthCategory.System,
                    Status = HealthStatus.Critical,
                    Message = $"Health check system failure: {ex.Message}",
                    Exception = ex
                });

                return report;
            }
        }

        /// <summary>
        /// Performs a quick health check with minimal overhead
        /// </summary>
        /// <returns>Health report with basic checks only</returns>
        public static async Task<HealthReport> QuickHealthCheckAsync()
        {
            var options = new HealthCheckOptions
            {
                IncludeSystemInfo = false,
                IncludePerformanceChecks = false,
                ValidateDataIntegrity = false,
                CheckTimeout = TimeSpan.FromSeconds(5),
                IncludeCategories = new List<HealthCategory>
                {
                    HealthCategory.Storage,
                    HealthCategory.Configuration,
                    HealthCategory.DiskSpace
                }
            };

            return await CheckHealthAsync(options);
        }

        /// <summary>
        /// Registers a custom health check
        /// </summary>
        /// <param name="healthCheck">Custom health check</param>
        public static void RegisterHealthCheck(IHealthCheck healthCheck)
        {
            lock (_lock)
            {
                _customChecks.Add(healthCheck);
            }
        }

        /// <summary>
        /// Removes a custom health check
        /// </summary>
        /// <param name="name">Name of health check to remove</param>
        public static void RemoveHealthCheck(string name)
        {
            lock (_lock)
            {
                _customChecks.RemoveAll(c => c.Name == name);
            }
        }

        /// <summary>
        /// Gets all registered custom health checks
        /// </summary>
        /// <returns>List of custom health checks</returns>
        public static List<IHealthCheck> GetCustomHealthChecks()
        {
            lock (_lock)
            {
                return _customChecks.ToList();
            }
        }

        /// <summary>
        /// Checks if the system is healthy (quick check)
        /// </summary>
        /// <returns>True if healthy, false otherwise</returns>
        public static async Task<bool> IsHealthyAsync()
        {
            var report = await QuickHealthCheckAsync();
            return report.IsHealthy;
        }

        #region Private Methods

        private static void CollectSystemInfo(HealthReport report)
        {
            try
            {
                report.SystemInfo["MachineName"] = Environment.MachineName;
                report.SystemInfo["OSVersion"] = Environment.OSVersion.ToString();
                report.SystemInfo["ProcessorCount"] = Environment.ProcessorCount;
                report.SystemInfo["WorkingSet"] = Environment.WorkingSet;
                report.SystemInfo["CLRVersion"] = Environment.Version.ToString();
                report.SystemInfo["Is64BitProcess"] = Environment.Is64BitProcess;
                report.SystemInfo["Is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem;
                report.SystemInfo["UserName"] = Environment.UserName;
            }
            catch (Exception ex)
            {
                report.SystemInfo["SystemInfoError"] = ex.Message;
            }
        }

        private static List<IHealthCheck> GetBuiltInHealthChecks(HealthCheckOptions options)
        {
            var checks = new List<IHealthCheck>();

            if (ShouldIncludeCategory(HealthCategory.Storage, options))
            {
                checks.Add(new StorageHealthCheck());
            }

            if (ShouldIncludeCategory(HealthCategory.Configuration, options))
            {
                checks.Add(new ConfigurationHealthCheck());
            }

            if (ShouldIncludeCategory(HealthCategory.DiskSpace, options))
            {
                checks.Add(new DiskSpaceHealthCheck());
            }

            if (ShouldIncludeCategory(HealthCategory.Memory, options))
            {
                checks.Add(new MemoryHealthCheck());
            }

            if (ShouldIncludeCategory(HealthCategory.Backup, options))
            {
                checks.Add(new BackupHealthCheck());
            }

            if (ShouldIncludeCategory(HealthCategory.Performance, options) && options.IncludePerformanceChecks)
            {
                checks.Add(new PerformanceHealthCheck());
            }

            if (ShouldIncludeCategory(HealthCategory.DataIntegrity, options) && options.ValidateDataIntegrity)
            {
                checks.Add(new DataIntegrityHealthCheck());
            }

            return checks;
        }

        private static List<IHealthCheck> GetCustomHealthChecks(HealthCheckOptions options)
        {
            lock (_lock)
            {
                return _customChecks.Where(check => ShouldIncludeCategory(check.Category, options)).ToList();
            }
        }

        private static bool ShouldIncludeCategory(HealthCategory category, HealthCheckOptions options)
        {
            if (options.ExcludeCategories?.Contains(category) == true)
                return false;

            if (options.IncludeCategories != null)
                return options.IncludeCategories.Contains(category);

            return true;
        }

        private static HealthStatus DetermineOverallStatus(List<HealthCheckResult> checks)
        {
            if (!checks.Any())
                return HealthStatus.Healthy;

            if (checks.Any(c => c.Status == HealthStatus.Critical))
                return HealthStatus.Critical;

            if (checks.Any(c => c.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            if (checks.Any(c => c.Status == HealthStatus.Warning))
                return HealthStatus.Warning;

            return HealthStatus.Healthy;
        }

        #endregion
    }
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
}