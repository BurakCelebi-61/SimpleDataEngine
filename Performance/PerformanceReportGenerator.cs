using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance report generator - Eksik GenerateReport metodu eklendi
    /// </summary>
    public static class PerformanceReportGenerator
    {
        private static readonly List<PerformanceMetric> _metrics = new List<PerformanceMetric>();
        private static readonly object _lock = new object();

        /// <summary>
        /// EKSIK OLAN METHOD - Generate performance report for specified time span
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
                TimeSpan = timeSpan,
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
            report.OverallSuccessRate = (double)successfulOperations.Count / metrics.Count * 100;

            // Calculate timing statistics
            if (successfulOperations.Count > 0)
            {
                var durations = successfulOperations.Select(m => m.Duration.TotalMilliseconds).ToList();

                report.AverageOperationTimeMs = durations.Average();
                report.MinOperationTimeMs = durations.Min();
                report.MaxOperationTimeMs = durations.Max();

                // Calculate percentiles
                durations.Sort();
                report.P50OperationTimeMs = GetPercentile(durations, 0.5);
                report.P95OperationTimeMs = GetPercentile(durations, 0.95);
                report.P99OperationTimeMs = GetPercentile(durations, 0.99);
            }

            // Calculate operations per second
            if (timeSpan.TotalSeconds > 0)
            {
                report.OperationsPerSecond = metrics.Count / timeSpan.TotalSeconds;
            }

            // Group by category
            report.CategoryBreakdown = metrics
                .GroupBy(m => m.Category)
                .ToDictionary(
                    g => g.Key,
                    g => new CategoryStatistics
                    {
                        TotalOperations = g.Count(),
                        SuccessfulOperations = g.Count(m => m.Success),
                        FailedOperations = g.Count(m => !m.Success),
                        AverageTimeMs = g.Where(m => m.Success).Select(m => m.Duration.TotalMilliseconds).DefaultIfEmpty().Average(),
                        SuccessRate = g.Count(m => m.Success) / (double)g.Count() * 100
                    });

            // Group by operation name
            report.OperationBreakdown = metrics
                .GroupBy(m => m.OperationName)
                .ToDictionary(
                    g => g.Key,
                    g => new OperationStatistics
                    {
                        TotalCalls = g.Count(),
                        SuccessfulCalls = g.Count(m => m.Success),
                        FailedCalls = g.Count(m => !m.Success),
                        AverageTimeMs = g.Where(m => m.Success).Select(m => m.Duration.TotalMilliseconds).DefaultIfEmpty().Average(),
                        MinTimeMs = g.Where(m => m.Success).Select(m => m.Duration.TotalMilliseconds).DefaultIfEmpty().Min(),
                        MaxTimeMs = g.Where(m => m.Success).Select(m => m.Duration.TotalMilliseconds).DefaultIfEmpty().Max(),
                        SuccessRate = g.Count(m => m.Success) / (double)g.Count() * 100
                    });

            // Identify slow operations (above 95th percentile)
            if (report.P95OperationTimeMs > 0)
            {
                report.SlowOperations = successfulOperations
                    .Where(m => m.Duration.TotalMilliseconds > report.P95OperationTimeMs)
                    .Select(m => new SlowOperation
                    {
                        OperationName = m.OperationName,
                        Category = m.Category,
                        Duration = m.Duration,
                        Timestamp = m.Timestamp
                    })
                    .OrderByDescending(so => so.Duration)
                    .Take(10)
                    .ToList();
            }

            return report;
        }

        /// <summary>
        /// Calculate percentile from sorted list
        /// </summary>
        private static double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;
            if (sortedValues.Count == 1) return sortedValues[0];

            double index = percentile * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);

            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];

            double weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight;
        }

        /// <summary>
        /// Get top performing operations
        /// </summary>
        /// <param name="timeSpan">Time span to analyze</param>
        /// <param name="count">Number of top operations to return</param>
        /// <returns>Top performing operations</returns>
        public static List<OperationStatistics> GetTopPerformingOperations(TimeSpan timeSpan, int count = 10)
        {
            var report = GenerateReport(timeSpan);

            return report.OperationBreakdown.Values
                .OrderBy(op => op.AverageTimeMs)
                .Where(op => op.SuccessRate > 90) // Only well-performing operations
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

            return report.OperationBreakdown.Values
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
                    ReportTimeSpan = report.TimeSpan,
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

    /// <summary>
    /// Performance metric data structure
    /// </summary>
    public class PerformanceMetric
    {
        public string OperationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance report data structure
    /// </summary>
    public class PerformanceReport
    {
        public TimeSpan TimeSpan { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double OverallSuccessRate { get; set; }
        public double AverageOperationTimeMs { get; set; }
        public double MinOperationTimeMs { get; set; }
        public double MaxOperationTimeMs { get; set; }
        public double P50OperationTimeMs { get; set; }
        public double P95OperationTimeMs { get; set; }
        public double P99OperationTimeMs { get; set; }
        public double OperationsPerSecond { get; set; }
        public Dictionary<string, CategoryStatistics> CategoryBreakdown { get; set; } = new Dictionary<string, CategoryStatistics>();
        public Dictionary<string, OperationStatistics> OperationBreakdown { get; set; } = new Dictionary<string, OperationStatistics>();
        public List<SlowOperation> SlowOperations { get; set; } = new List<SlowOperation>();

        public override string ToString()
        {
            return $"PerformanceReport[Operations: {TotalOperations}, Success Rate: {OverallSuccessRate:F1}%, Avg Time: {AverageOperationTimeMs:F2}ms]";
        }
    }

    /// <summary>
    /// Category statistics
    /// </summary>
    public class CategoryStatistics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double AverageTimeMs { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Operation statistics
    /// </summary>
    public class OperationStatistics
    {
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public double AverageTimeMs { get; set; }
        public double MinTimeMs { get; set; }
        public double MaxTimeMs { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Slow operation details
    /// </summary>
    public class SlowOperation
    {
        public string OperationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Performance tracker utility for measuring operations
    /// </summary>
    public static class PerformanceTracker
    {
        /// <summary>
        /// Start tracking an operation
        /// </summary>
        /// <param name="operationName">Operation name</param>
        /// <param name="category">Operation category</param>
        /// <returns>Disposable performance tracker</returns>
        public static IDisposable StartOperation(string operationName, string category = "General")
        {
            return new OperationTracker(operationName, category);
        }

        /// <summary>
        /// Track an async operation
        /// </summary>
        /// <param name="operationName">Operation name</param>
        /// <param name="operation">Operation to track</param>
        /// <param name="category">Operation category</param>
        /// <returns>Operation result</returns>
        public static async Task<T> TrackAsync<T>(string operationName, Func<Task<T>> operation, string category = "General")
        {
            using var tracker = StartOperation(operationName, category);
            try
            {
                var result = await operation();
                tracker.MarkSuccess();
                return result;
            }
            catch
            {
                tracker.MarkFailure();
                throw;
            }
        }

        /// <summary>
        /// Track a synchronous operation
        /// </summary>
        /// <param name="operationName">Operation name</param>
        /// <param name="operation">Operation to track</param>
        /// <param name="category">Operation category</param>
        /// <returns>Operation result</returns>
        public static T Track<T>(string operationName, Func<T> operation, string category = "General")
        {
            using var tracker = StartOperation(operationName, category);
            try
            {
                var result = operation();
                tracker.MarkSuccess();
                return result;
            }
            catch
            {
                tracker.MarkFailure();
                throw;
            }
        }
    }

    /// <summary>
    /// Operation tracker implementation
    /// </summary>
    public class OperationTracker : IDisposable
    {
        private readonly string _operationName;
        private readonly string _category;
        private readonly DateTime _startTime;
        private bool _success = true;
        private bool _disposed = false;

        public OperationTracker(string operationName, string category)
        {
            _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _category = category ?? "General";
            _startTime = DateTime.UtcNow;
        }

        public void MarkSuccess()
        {
            _success = true;
        }

        public void MarkFailure()
        {
            _success = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                PerformanceReportGenerator.RecordMetric(_operationName, duration, _category, _success);
                _disposed = true;
            }
        }
    }
}