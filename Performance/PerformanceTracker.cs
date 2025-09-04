using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Static performance tracker for global performance monitoring
    /// </summary>
    public static class PerformanceTracker
    {
        private static readonly List<PerformanceMetric> _metrics = new();
        private static readonly object _lock = new object();
        private static Timer _cleanupTimer;
        private static bool _initialized = false;

        static PerformanceTracker()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the performance tracker
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                // Cleanup old metrics every hour
                _cleanupTimer = new Timer(CleanupOldMetrics, null,
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1));

                _initialized = true;
            }
        }

        /// <summary>
        /// Records a performance metric
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="category">Category (default: General)</param>
        /// <param name="success">Whether operation succeeded</param>
        /// <param name="errorMessage">Error message if failed</param>
        public static void TrackOperation(string operation, TimeSpan duration,
            string category = "General", bool success = true, string errorMessage = null)
        {
            if (!ConfigManager.Current.Performance.EnablePerformanceTracking)
                return;

            if (string.IsNullOrWhiteSpace(operation))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operation));

            lock (_lock)
            {
                _metrics.Add(new PerformanceMetric
                {
                    Operation = operation,
                    Timestamp = DateTime.Now,
                    Duration = duration,
                    MemoryUsed = GC.GetTotalMemory(false),
                    Category = category ?? "General",
                    Success = success,
                    ErrorMessage = errorMessage,
                    ThreadId = Thread.CurrentThread.ManagedThreadId
                });

                // Check for slow operations
                var slowThreshold = ConfigManager.Current.Performance.SlowQueryThresholdMs;
                if (duration.TotalMilliseconds > slowThreshold)
                {
                    OnSlowOperationDetected(operation, duration, category);
                }
            }
        }

        /// <summary>
        /// Starts tracking an operation and returns a disposable tracker
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="category">Category (default: General)</param>
        /// <returns>Operation timer that tracks when disposed</returns>
        public static IDisposable StartOperation(string operation, string category = "General")
        {
            return new StaticOperationTimer(operation, category);
        }

        /// <summary>
        /// Gets all metrics within the specified time period
        /// </summary>
        /// <param name="period">Time period (default: last 24 hours)</param>
        /// <returns>List of metrics</returns>
        public static List<PerformanceMetric> GetMetrics(TimeSpan? period = null)
        {
            lock (_lock)
            {
                var reportPeriod = period ?? TimeSpan.FromHours(24);
                var cutoff = DateTime.Now - reportPeriod;

                return _metrics
                    .Where(m => m.Timestamp >= cutoff)
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets current performance report for health checks
        /// </summary>
        /// <returns>Current performance report</returns>
        public static PerformanceReport GetCurrentReport()
        {
            lock (_lock)
            {
                var metrics = GetMetrics(TimeSpan.FromHours(1)); // Son 1 saatlik data

                var report = new PerformanceReport
                {
                    GeneratedAt = DateTime.Now,
                    ReportPeriod = TimeSpan.FromHours(1),
                    TotalOperations = metrics.Count,
                    SuccessfulOperations = metrics.Count(m => m.Success),
                    FailedOperationsCount = metrics.Count(m => !m.Success),
                    SlowOperations = GetSlowestOperations(10, TimeSpan.FromHours(1)),
                    FailedOperations = GetFailedOperations(TimeSpan.FromHours(1))
                };

                if (report.TotalOperations > 0)
                {
                    report.OverallSuccessRate = (double)report.SuccessfulOperations / report.TotalOperations * 100;
                    report.AverageOperationTimeMs = metrics.Average(m => m.Duration.TotalMilliseconds);

                    var timeSpan = report.ReportPeriod.TotalSeconds;
                    report.OperationsPerSecond = timeSpan > 0 ? report.TotalOperations / timeSpan : 0;
                }

                return report;
            }
        }

        /// <summary>
        /// Gets metrics for a specific operation
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="period">Time period (default: last 24 hours)</param>
        /// <returns>List of metrics for the operation</returns>
        public static List<PerformanceMetric> GetOperationMetrics(string operation, TimeSpan? period = null)
        {
            lock (_lock)
            {
                var reportPeriod = period ?? TimeSpan.FromHours(24);
                var cutoff = DateTime.Now - reportPeriod;

                return _metrics
                    .Where(m => m.Timestamp >= cutoff &&
                               string.Equals(m.Operation, operation, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets metrics for a specific category
        /// </summary>
        /// <param name="category">Category name</param>
        /// <param name="period">Time period (default: last 24 hours)</param>
        /// <returns>List of metrics for the category</returns>
        public static List<PerformanceMetric> GetCategoryMetrics(string category, TimeSpan? period = null)
        {
            lock (_lock)
            {
                var reportPeriod = period ?? TimeSpan.FromHours(24);
                var cutoff = DateTime.Now - reportPeriod;

                return _metrics
                    .Where(m => m.Timestamp >= cutoff &&
                               string.Equals(m.Category, category, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the slowest operations within the specified period
        /// </summary>
        /// <param name="count">Number of slowest operations to return</param>
        /// <param name="period">Time period (default: last 24 hours)</param>
        /// <returns>List of slowest operations</returns>
        public static List<PerformanceMetric> GetSlowestOperations(int count = 10, TimeSpan? period = null)
        {
            lock (_lock)
            {
                var reportPeriod = period ?? TimeSpan.FromHours(24);
                var cutoff = DateTime.Now - reportPeriod;

                return _metrics
                    .Where(m => m.Timestamp >= cutoff)
                    .OrderByDescending(m => m.Duration)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets operations with errors within the specified period
        /// </summary>
        /// <param name="period">Time period (default: last 24 hours)</param>
        /// <returns>List of failed operations</returns>
        public static List<PerformanceMetric> GetFailedOperations(TimeSpan? period = null)
        {
            lock (_lock)
            {
                var reportPeriod = period ?? TimeSpan.FromHours(24);
                var cutoff = DateTime.Now - reportPeriod;

                return _metrics
                    .Where(m => m.Timestamp >= cutoff && !m.Success)
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets performance statistics for an operation
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="period">Time period (default: last 24 hours)</param>
        /// <returns>Performance statistics</returns>
        public static OperationStatistics GetOperationStatistics(string operation, TimeSpan? period = null)
        {
            var metrics = GetOperationMetrics(operation, period);

            if (!metrics.Any())
            {
                return new OperationStatistics
                {
                    Operation = operation,
                    TotalCalls = 0
                };
            }

            var durations = metrics.Select(m => m.Duration.TotalMilliseconds).ToList();
            var successfulCalls = metrics.Count(m => m.Success);

            return new OperationStatistics
            {
                Operation = operation,
                TotalCalls = metrics.Count,
                SuccessfulCalls = successfulCalls,
                FailedCalls = metrics.Count - successfulCalls,
                SuccessRate = (double)successfulCalls / metrics.Count * 100,
                AverageResponseTime = durations.Average(),
                MinResponseTime = durations.Min(),
                MaxResponseTime = durations.Max(),
                MedianResponseTime = GetMedian(durations),
                FirstCall = metrics.Min(m => m.Timestamp),
                LastCall = metrics.Max(m => m.Timestamp)
            };
        }

        /// <summary>
        /// Clears all performance metrics
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _metrics.Clear();
            }
        }

        /// <summary>
        /// Gets the total number of tracked metrics
        /// </summary>
        public static int MetricCount
        {
            get
            {
                lock (_lock)
                {
                    return _metrics.Count;
                }
            }
        }

        /// <summary>
        /// Event fired when a slow operation is detected
        /// </summary>
        public static event Action<string, TimeSpan, string> SlowOperationDetected;

        private static void OnSlowOperationDetected(string operation, TimeSpan duration, string category)
        {
            SlowOperationDetected?.Invoke(operation, duration, category);
        }

        private static void CleanupOldMetrics(object state)
        {
            lock (_lock)
            {
                try
                {
                    var cutoff = DateTime.Now - TimeSpan.FromDays(7); // Keep 7 days of data
                    var oldMetrics = _metrics.Where(m => m.Timestamp < cutoff).ToList();

                    foreach (var metric in oldMetrics)
                    {
                        _metrics.Remove(metric);
                    }
                }
                catch (Exception)
                {
                    // Ignore cleanup errors
                }
            }
        }

        private static double GetMedian(List<double> values)
        {
            if (!values.Any()) return 0;

            var sorted = values.OrderBy(x => x).ToList();
            int count = sorted.Count;

            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
            }
            else
            {
                return sorted[count / 2];
            }
        }

        /// <summary>
        /// Dispose resources used by the performance tracker
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                _cleanupTimer?.Dispose();
                _initialized = false;
            }
        }
    }
}