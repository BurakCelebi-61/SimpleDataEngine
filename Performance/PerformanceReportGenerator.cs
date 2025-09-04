using System.Diagnostics;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance report generator
    /// </summary>
    public static class PerformanceReportGenerator
    {
        /// <summary>
        /// Generates a comprehensive performance report using static PerformanceTracker
        /// </summary>
        /// <param name="period">Report period</param>
        /// <returns>Performance report</returns>
        public static PerformanceReport GenerateReport(TimeSpan? period = null)
        {
            var reportPeriod = period ?? TimeSpan.FromHours(24);
            var metrics = PerformanceTracker.GetMetrics(reportPeriod);

            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.Now,
                ReportPeriod = reportPeriod,
                TotalOperations = metrics.Count,
                SuccessfulOperations = metrics.Count(m => m.Success),
                FailedOperationsCount = metrics.Count(m => !m.Success) // Fixed property name
            };

            if (report.TotalOperations > 0)
            {
                report.OverallSuccessRate = (double)report.SuccessfulOperations / report.TotalOperations * 100;
                report.AverageOperationTimeMs = metrics.Average(m => m.Duration.TotalMilliseconds);

                var timeSpan = reportPeriod.TotalSeconds;
                report.OperationsPerSecond = timeSpan > 0 ? report.TotalOperations / timeSpan : 0;
            }

            // Generate operation statistics using static methods
            GenerateOperationStatistics(report, metrics, reportPeriod);

            // Generate category statistics
            GenerateCategoryStatistics(report, metrics);

            // Get slowest operations using static methods
            report.SlowOperations = PerformanceTracker.GetSlowestOperations(10, reportPeriod);

            // Get failed operations using static methods
            report.FailedOperations = PerformanceTracker.GetFailedOperations(reportPeriod);

            // Generate resource statistics
            GenerateResourceStatistics(report, metrics);

            // Generate alerts
            GenerateAlerts(report, metrics);

            return report;
        }

        private static void GenerateOperationStatistics(PerformanceReport report,
            List<PerformanceMetric> metrics, TimeSpan period)
        {
            var operations = metrics.Select(m => m.Operation).Distinct();

            foreach (var operation in operations)
            {
                var stats = PerformanceTracker.GetOperationStatistics(operation, period);
                report.OperationStats[operation] = stats;
            }
        }

        private static void GenerateCategoryStatistics(PerformanceReport report, List<PerformanceMetric> metrics)
        {
            var categories = metrics.GroupBy(m => m.Category);

            foreach (var categoryGroup in categories)
            {
                var categoryMetrics = categoryGroup.ToList();
                var successfulOps = categoryMetrics.Count(m => m.Success);

                var categoryStats = new CategoryStatistics
                {
                    Category = categoryGroup.Key,
                    TotalOperations = categoryMetrics.Count,
                    SuccessfulOperations = successfulOps,
                    FailedOperations = categoryMetrics.Count - successfulOps,
                    SuccessRate = categoryMetrics.Count > 0 ? (double)successfulOps / categoryMetrics.Count * 100 : 0,
                    AverageResponseTime = categoryMetrics.Count > 0 ? categoryMetrics.Average(m => m.Duration.TotalMilliseconds) : 0,
                    TotalMemoryUsed = categoryMetrics.Sum(m => m.MemoryUsed),
                    TopOperations = categoryMetrics
                        .GroupBy(m => m.Operation)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToList()
                };

                report.CategoryStats[categoryGroup.Key] = categoryStats;
            }
        }

        private static void GenerateResourceStatistics(PerformanceReport report, List<PerformanceMetric> metrics)
        {
            if (!metrics.Any())
            {
                report.Resources = new ResourceStatistics();
                return;
            }

            report.Resources = new ResourceStatistics
            {
                CurrentMemoryUsage = GC.GetTotalMemory(false),
                PeakMemoryUsage = metrics.Max(m => m.MemoryUsed),
                AverageMemoryUsage = (long)metrics.Average(m => m.MemoryUsed),
                GarbageCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
                ActiveThreads = metrics.Select(m => m.ThreadId).Distinct().Count(),
                SystemUpTime = DateTime.Now - Process.GetCurrentProcess().StartTime
            };
        }

        private static void GenerateAlerts(PerformanceReport report, List<PerformanceMetric> metrics)
        {
            var config = Configuration.ConfigManager.Current;

            // Check for high error rate
            if (report.TotalOperations > 0 && report.OverallSuccessRate < 95)
            {
                report.Alerts.Add(new PerformanceAlert
                {
                    Level = PerformanceAlert.AlertLevel.Warning,
                    Title = "High Error Rate",
                    Message = $"Overall success rate is {report.OverallSuccessRate:F2}% (below 95%)",
                    Category = "Reliability",
                    DetectedAt = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["SuccessRate"] = report.OverallSuccessRate,
                        ["FailedOperationsCount"] = report.FailedOperationsCount
                    }
                });
            }

            // Check for slow average response time
            if (report.AverageOperationTimeMs > config.Performance.SlowQueryThresholdMs)
            {
                report.Alerts.Add(new PerformanceAlert
                {
                    Level = PerformanceAlert.AlertLevel.Warning,
                    Title = "Slow Average Response Time",
                    Message = $"Average response time is {report.AverageOperationTimeMs:F2}ms (above {config.Performance.SlowQueryThresholdMs}ms)",
                    Category = "Performance",
                    DetectedAt = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["AverageResponseTime"] = report.AverageOperationTimeMs,
                        ["Threshold"] = config.Performance.SlowQueryThresholdMs
                    }
                });
            }

            // Check for memory usage
            var memoryMB = report.Resources.CurrentMemoryUsage / (1024 * 1024);
            if (memoryMB > 500)
            {
                var level = memoryMB > 1000 ? PerformanceAlert.AlertLevel.Critical : PerformanceAlert.AlertLevel.Warning;
                report.Alerts.Add(new PerformanceAlert
                {
                    Level = level,
                    Title = "High Memory Usage",
                    Message = $"Current memory usage is {memoryMB:F2} MB",
                    Category = "Resources",
                    DetectedAt = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["MemoryUsageMB"] = memoryMB,
                        ["CurrentMemoryUsage"] = report.Resources.CurrentMemoryUsage
                    }
                });
            }

            // Check for operations with consistently poor performance
            foreach (var opStat in report.OperationStats.Values)
            {
                if (opStat.TotalCalls >= 10 && opStat.SuccessRate < 90)
                {
                    report.Alerts.Add(new PerformanceAlert
                    {
                        Level = PerformanceAlert.AlertLevel.Warning,
                        Title = "Unreliable Operation",
                        Message = $"Operation '{opStat.Operation}' has {opStat.SuccessRate:F2}% success rate",
                        Category = "Reliability",
                        DetectedAt = DateTime.Now,
                        Data = new Dictionary<string, object>
                        {
                            ["Operation"] = opStat.Operation,
                            ["SuccessRate"] = opStat.SuccessRate,
                            ["TotalCalls"] = opStat.TotalCalls,
                            ["FailedCalls"] = opStat.FailedCalls
                        }
                    });
                }

                if (opStat.AverageResponseTime > config.Performance.SlowQueryThresholdMs * 2)
                {
                    report.Alerts.Add(new PerformanceAlert
                    {
                        Level = PerformanceAlert.AlertLevel.Critical,
                        Title = "Very Slow Operation",
                        Message = $"Operation '{opStat.Operation}' averages {opStat.AverageResponseTime:F2}ms",
                        Category = "Performance",
                        DetectedAt = DateTime.Now,
                        Data = new Dictionary<string, object>
                        {
                            ["Operation"] = opStat.Operation,
                            ["AverageResponseTime"] = opStat.AverageResponseTime,
                            ["MaxResponseTime"] = opStat.MaxResponseTime,
                            ["TotalCalls"] = opStat.TotalCalls
                        }
                    });
                }
            }
        }
    }
}