using SimpleDataEngine.Audit;
using System.Diagnostics;
using System.Text.Json;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Operation timer for performance monitoring
    /// </summary>
    public class OperationTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;
        private readonly Dictionary<string, object> _metadata;
        private bool _disposed;

        public OperationTimer(string operationName, Dictionary<string, object> metadata = null)
        {
            _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _metadata = metadata ?? new Dictionary<string, object>();
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Current elapsed time
        /// </summary>
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        /// <summary>
        /// Operation name
        /// </summary>
        public string OperationName => _operationName;

        /// <summary>
        /// Is timer running
        /// </summary>
        public bool IsRunning => _stopwatch.IsRunning;

        /// <summary>
        /// Add metadata to the operation
        /// </summary>
        public void AddMetadata(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _metadata[key] = value;
            }
        }

        /// <summary>
        /// Stop the timer manually
        /// </summary>
        public PerformanceResult Stop()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
            }

            return new PerformanceResult
            {
                OperationName = _operationName,
                Duration = _stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>(_metadata),
                StartTime = DateTime.UtcNow - _stopwatch.Elapsed,
                EndTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Stop and log the performance
        /// </summary>
        public async Task<PerformanceResult> StopAndLogAsync()
        {
            var result = Stop();

            await AuditLogger.LogPerformanceAsync(_operationName, result.Duration, new
            {
                Metadata = _metadata,
                StartTime = result.StartTime,
                EndTime = result.EndTime
            });

            return result;
        }

        /// <summary>
        /// Create a child timer for sub-operations
        /// </summary>
        public OperationTimer CreateChildTimer(string childOperationName)
        {
            var childMetadata = new Dictionary<string, object>(_metadata)
            {
                ["ParentOperation"] = _operationName,
                ["ParentStartTime"] = DateTime.UtcNow - _stopwatch.Elapsed
            };

            return new OperationTimer($"{_operationName}.{childOperationName}", childMetadata);
        }

        /// <summary>
        /// Checkpoint - record intermediate timing
        /// </summary>
        public void Checkpoint(string checkpointName)
        {
            _metadata[$"Checkpoint_{checkpointName}"] = _stopwatch.Elapsed.TotalMilliseconds;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_stopwatch.IsRunning)
                {
                    _stopwatch.Stop();

                    // Auto-log when disposed
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await AuditLogger.LogPerformanceAsync(_operationName, _stopwatch.Elapsed, _metadata);
                        }
                        catch
                        {
                            // Ignore logging errors during disposal
                        }
                    });
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Static factory methods for common operations
        /// </summary>
        public static OperationTimer StartDatabaseOperation(string operation, string entityName = null)
        {
            var metadata = new Dictionary<string, object>
            {
                ["Category"] = "Database",
                ["Operation"] = operation
            };

            if (!string.IsNullOrEmpty(entityName))
            {
                metadata["EntityName"] = entityName;
            }

            return new OperationTimer($"DB.{operation}", metadata);
        }

        public static OperationTimer StartFileOperation(string operation, string filePath = null)
        {
            var metadata = new Dictionary<string, object>
            {
                ["Category"] = "File",
                ["Operation"] = operation
            };

            if (!string.IsNullOrEmpty(filePath))
            {
                metadata["FilePath"] = filePath;
            }

            return new OperationTimer($"File.{operation}", metadata);
        }

        public static OperationTimer StartIndexOperation(string operation, string entityName = null, string propertyName = null)
        {
            var metadata = new Dictionary<string, object>
            {
                ["Category"] = "Index",
                ["Operation"] = operation
            };

            if (!string.IsNullOrEmpty(entityName))
                metadata["EntityName"] = entityName;

            if (!string.IsNullOrEmpty(propertyName))
                metadata["PropertyName"] = propertyName;

            return new OperationTimer($"Index.{operation}", metadata);
        }
    }

    /// <summary>
    /// Performance result data
    /// </summary>
    public class PerformanceResult
    {
        public string OperationName { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public double DurationMs => Duration.TotalMilliseconds;

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public double DurationSeconds => Duration.TotalSeconds;

        /// <summary>
        /// Performance category based on duration
        /// </summary>
        public PerformanceCategory Category
        {
            get
            {
                return DurationMs switch
                {
                    < 10 => PerformanceCategory.Excellent,
                    < 100 => PerformanceCategory.Good,
                    < 1000 => PerformanceCategory.Acceptable,
                    < 5000 => PerformanceCategory.Slow,
                    _ => PerformanceCategory.VerySlow
                };
            }
        }

        public override string ToString()
        {
            return $"{OperationName}: {DurationMs:F2}ms ({Category})";
        }
    }

    /// <summary>
    /// Performance categories
    /// </summary>
    public enum PerformanceCategory
    {
        Excellent,
        Good,
        Acceptable,
        Slow,
        VerySlow
    }

    

    /// <summary>
    /// Performance summary data
    /// </summary>
    public class PerformanceSummary
    {
        public int TotalOperations { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan MedianDuration { get; set; }
        public TimeSpan P95Duration { get; set; }
        public TimeSpan P99Duration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan? TimeWindow { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<OperationSummary> OperationSummaries { get; set; } = new();
        public Dictionary<PerformanceCategory, CategoryStats> CategoryBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Operation summary data
    /// </summary>
    public class OperationSummary
    {
        public string OperationName { get; set; }
        public int Count { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    /// <summary>
    /// Category statistics
    /// </summary>
    public class CategoryStats
    {
        public int Count { get; set; }
        public double Percentage { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }
}