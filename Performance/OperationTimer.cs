using SimpleDataEngine.Audit;
using System.Diagnostics;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Operation timer for performance monitoring
    /// </summary>
    public class OperationTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, object> _metadata;
        private bool _disposed;

        public OperationTimer(string operationName, Dictionary<string, object> metadata = null)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
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
        public string OperationName { get; }

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
                OperationName = OperationName,
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

            await AuditLogger.LogPerformanceAsync(OperationName, result.Duration, new
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
                ["ParentOperation"] = OperationName,
                ["ParentStartTime"] = DateTime.UtcNow - _stopwatch.Elapsed
            };

            return new OperationTimer($"{OperationName}.{childOperationName}", childMetadata);
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
                            await AuditLogger.LogPerformanceAsync(OperationName, _stopwatch.Elapsed, _metadata);
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
}