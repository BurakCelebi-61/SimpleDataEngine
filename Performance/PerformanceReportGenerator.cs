using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance report generator - Fixed all issues
    /// </summary>
    public static class PerformanceReportGenerator
    {
        private static readonly List<PerformanceMetric> _metrics = new List<PerformanceMetric>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Generate performance report for specified time span
        /// </summary>
        /// <param name="timeSpan">Time span to analyze</param>
        /// <returns>Performance report</returns>
        public static PerformanceReport GenerateReport(TimeSpan timeSpan)
        {
            lock (_lock)
            {
                var cutoffTime = DateTime.UtcNow - timeSpan;
                var relevantMetrics = _metrics.Where(m => m.Timestamp >= cutoffTime).ToList();

                return GenerateReportFromMetrics(relevantMetrics, timeSpan);
            }
        }

        /// <summary>
        /// Generate performance report for date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Performance report</returns>
        public static PerformanceReport GenerateReport(DateTime startDate, DateTime endDate)
        {
            lock (_lock)
            {
                var relevantMetrics = _metrics.Where(m => m.Timestamp >= startDate && m.Timestamp <= endDate).ToList();
                var timeSpan = endDate - startDate;

                return GenerateReportFromMetrics(relevantMetrics, timeSpan);
            }
        }

        /// <summary>
        /// Generate current performance report (last hour)
        /// </summary>
        /// <returns>Performance report</returns>
        public static PerformanceReport GenerateCurrentReport()
        {
            return GenerateReport(TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Record a performance metric
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="category">Operation category</param>
        /// <param name="success">Whether operation was successful</param>
        public static void RecordMetric(string operationName, TimeSpan duration, string category = "General", bool success = true)
        {
            var metric = new PerformanceMetric
            {
                OperationName = operationName,
                Category = category,
                Duration = duration,
                Success = success,
                Timestamp = DateTime.UtcNow
            };

            lock (_lock)
            {
                _metrics.Add(metric);

                // Keep only recent metrics to prevent memory bloat
                if (_metrics.Count > 10000)
                {
                    var cutoff = DateTime.UtcNow.AddHours(-24);
                    _metrics.RemoveAll(m => m.Timestamp < cutoff);
                }
            }

            // Log to audit system
            AuditLogger.LogTimed($"PERFORMANCE_{operationName.ToUpper()}", duration, new
            {
                Category = category,
                Success = success
            }, AuditCategory.Performance);
        }

        /// <summary>
        /// Clear all recorded metrics
        /// </summary>
        public static void ClearMetrics()
        {
            lock (_lock)
            {
                _metrics.Clear();
            }

            AuditLogger.Log("PERFORMANCE_METRICS_CLEARED", null, AuditCategory.Performance);
        }

        /// <summary>
        /// Get metrics count
        /// </summary>
        /// <returns>Number of recorded metrics</returns>
        public static int GetMetricsCount()
        {
            lock (_lock)
            {
                return _metrics.Count;
            }
        }

        /// <summary>
        /// Generate report from metrics collection
        /// </summary>
        private static PerformanceReport GenerateReportFromMetrics(List<PerformanceMetric> metrics, TimeSpan timeSpan)
        {
            var report = new PerformanceReport
            {
                ReportPeriod = timeSpan,
                GeneratedAt = DateTime.UtcNow,
                TotalOperations = metrics.Count
            };

            if (metrics.Count == 0)
            {
                return report;
            }

            // Calculate overall statistics
            var successfulOperations = metrics.Where(m => m.Success).ToList();
            var failedOperations = metrics.Where(m => !m.Success).ToList();

            report.SuccessfulOperations = successfulOperations.Count;
            report.FailedOperations = failedOperations.Count;
            report.OverallSuccessRate = metrics.Count > 0 ? (double)successfulOperations.Count / metrics.Count * 100.0 : 0.0;

            // Calculate average operation time
            if (metrics.Count > 0)
            {
                report.AverageOperationTimeMs = metrics.Average(m => m.Duration.TotalMilliseconds);
            }

            // Calculate operations per second
            if (timeSpan.TotalSeconds > 0)
            {
                report.OperationsPerSecond = metrics.Count / timeSpan.TotalSeconds;
            }

            // Generate category statistics
            var categoryGroups = metrics.GroupBy(m => m.Category);
            foreach (var group in categoryGroups)
            {
                var categoryMetrics = group.ToList();
                var categorySuccessful = categoryMetrics.Count(m => m.Success);

                report.CategoryStats[group.Key] = new CategoryStatistics
                {
                    TotalOperations = categoryMetrics.Count,
                    SuccessfulOperations = categorySuccessful,
                    FailedOperations = categoryMetrics.Count - categorySuccessful,
                    AverageTimeMs = categoryMetrics.Average(m => m.Duration.TotalMilliseconds),
                    SuccessRate = categoryMetrics.Count > 0 ? (double)categorySuccessful / categoryMetrics.Count * 100.0 : 0.0
                };
            }

            // Generate operation statistics
            var operationGroups = metrics.GroupBy(m => m.OperationName);
            foreach (var group in operationGroups)
            {
                var operationMetrics = group.ToList();
                var operationSuccessful = operationMetrics.Count(m => m.Success);
                var operationFailed = operationMetrics.Count(m => !m.Success);

                report.OperationStats[group.Key] = new OperationStatistics
                {
                    TotalCalls = operationMetrics.Count,
                    SuccessfulCalls = operationSuccessful,
                    FailedCalls = operationFailed,
                    AverageTimeMs = operationMetrics.Average(m => m.Duration.TotalMilliseconds),
                    MinTimeMs = operationMetrics.Min(m => m.Duration.TotalMilliseconds),
                    MaxTimeMs = operationMetrics.Max(m => m.Duration.TotalMilliseconds),
                    SuccessRate = operationMetrics.Count > 0 ? (double)operationSuccessful / operationMetrics.Count * 100.0 : 0.0
                };
            }

            // Find slow operations (> 1000ms)
            report.SlowOperations = metrics
                .Where(m => m.Duration.TotalMilliseconds > 1000)
                .OrderByDescending(m => m.Duration.TotalMilliseconds)
                .Take(10)
                .ToList();

            // Find failed operations
            report.FailedOperationsList = failedOperations
                .OrderByDescending(m => m.Timestamp)
                .Take(10)
                .ToList();

            return report;
        }

        /// <summary>
        /// Get best performing operations
        /// </summary>
        /// <param name="timeSpan">Time span to analyze</param>
        /// <param name="count">Number of best operations to return</param>
        /// <returns>Best performing operations</returns>
        public static List<OperationStatistics> GetBestPerformingOperations(TimeSpan timeSpan, int count = 10)
        {
            var report = GenerateReport(timeSpan);

            return report.OperationStats.Values
                .Where(op => op.SuccessRate > 95.0)
                .OrderBy(op => op.AverageTimeMs)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get worst performing operations
        /// </summary>
        /// <param name="timeSpan">Time span to analyze</param>
        /// <param name="count">Number of worst operations to return</param>
        /// <returns>Worst performing operations</returns>
        public static List<OperationStatistics> GetWorstPerformingOperations(TimeSpan timeSpan, int count = 10)
        {
            var report = GenerateReport(timeSpan);

            return report.OperationStats.Values
                .OrderByDescending(op => op.AverageTimeMs)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Export report to JSON
        /// </summary>
        /// <param name="report">Performance report</param>
        /// <param name="filePath">File path to save</param>
        public static async Task ExportReportAsync(PerformanceReport report, string filePath)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, json);

                AuditLogger.Log("PERFORMANCE_REPORT_EXPORTED", new
                {
                    FilePath = filePath,
                    ReportTimeSpan = report.ReportPeriod,
                    TotalOperations = report.TotalOperations
                }, AuditCategory.Performance);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("PERFORMANCE_REPORT_EXPORT_FAILED", ex, new { FilePath = filePath });
                throw;
            }
        }
    }
}