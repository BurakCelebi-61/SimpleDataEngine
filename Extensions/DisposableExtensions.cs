namespace SimpleDataEngine.Extensions
{
    /// <summary>
    /// Extension methods for IDisposable to support MarkSuccess/MarkFailure operations
    /// </summary>
    public static class DisposableExtensions
    {
        private static readonly Dictionary<object, OperationContext> _operationContexts = new();
        private static readonly object _lock = new object();

        /// <summary>
        /// Marks the operation as successful
        /// </summary>
        /// <param name="disposable">The disposable object</param>
        /// <param name="message">Optional success message</param>
        public static void MarkSuccess(this IDisposable disposable, string? message = null)
        {
            lock (_lock)
            {
                if (_operationContexts.TryGetValue(disposable, out var context))
                {
                    context.Success = true;
                    context.Message = message ?? "Operation completed successfully";
                    context.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new context if not exists
                    _operationContexts[disposable] = new OperationContext
                    {
                        Success = true,
                        Message = message ?? "Operation completed successfully",
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                }
            }
        }

        /// <summary>
        /// Marks the operation as failed
        /// </summary>
        /// <param name="disposable">The disposable object</param>
        /// <param name="message">Error message</param>
        /// <param name="exception">Optional exception details</param>
        public static void MarkFailure(this IDisposable disposable, string message, Exception? exception = null)
        {
            lock (_lock)
            {
                if (_operationContexts.TryGetValue(disposable, out var context))
                {
                    context.Success = false;
                    context.Message = message;
                    context.Exception = exception;
                    context.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new context if not exists
                    _operationContexts[disposable] = new OperationContext
                    {
                        Success = false,
                        Message = message,
                        Exception = exception,
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                }
            }
        }

        /// <summary>
        /// Gets the operation context for a disposable
        /// </summary>
        /// <param name="disposable">The disposable object</param>
        /// <returns>Operation context or null if not found</returns>
        public static OperationContext? GetOperationContext(this IDisposable disposable)
        {
            lock (_lock)
            {
                return _operationContexts.TryGetValue(disposable, out var context) ? context : null;
            }
        }

        /// <summary>
        /// Creates a tracked operation scope
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="data">Optional operation data</param>
        /// <returns>Tracked disposable operation</returns>
        public static TrackedOperation CreateTrackedOperation(string operationName, object? data = null)
        {
            return new TrackedOperation(operationName, data);
        }

        /// <summary>
        /// Cleans up completed operation contexts
        /// </summary>
        internal static void CleanupCompletedOperations()
        {
            lock (_lock)
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-1); // Remove contexts older than 1 hour
                var keysToRemove = _operationContexts
                    .Where(kvp => kvp.Value.CompletedAt.HasValue && kvp.Value.CompletedAt < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _operationContexts.Remove(key);
                }
            }
        }
    }

    /// <summary>
    /// Context information for tracked operations
    /// </summary>
    public class OperationContext
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Operation message (success or error)
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception details if operation failed
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// When the operation started
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// When the operation completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Operation duration
        /// </summary>
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt - StartedAt : null;

        /// <summary>
        /// Additional operation data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Tracked disposable operation
    /// </summary>
    public class TrackedOperation : IDisposable
    {
        private readonly OperationContext _context;
        private bool _disposed = false;

        /// <summary>
        /// Operation name
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Operation context
        /// </summary>
        public OperationContext Context => _context;

        internal TrackedOperation(string operationName, object? data = null)
        {
            OperationName = operationName;
            _context = new OperationContext
            {
                StartedAt = DateTime.UtcNow,
                Message = $"Operation '{operationName}' started"
            };

            if (data != null)
            {
                _context.Data["InitialData"] = data;
            }

            // Register this operation
            lock (DisposableExtensions._lock)
            {
                DisposableExtensions._operationContexts[this] = _context;
            }
        }

        /// <summary>
        /// Disposes the operation and logs completion
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // If not explicitly marked as success/failure, consider it successful
                if (!_context.CompletedAt.HasValue)
                {
                    this.MarkSuccess($"Operation '{OperationName}' completed");
                }

                // Log the operation result
                var duration = _context.Duration?.TotalMilliseconds ?? 0;
                var logMessage = $"Operation '{OperationName}' {(_context.Success ? "succeeded" : "failed")} in {duration:F2}ms";

                if (!_context.Success && _context.Exception != null)
                {
                    logMessage += $" - Error: {_context.Exception.Message}";
                }

                // Could integrate with audit logging here
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {logMessage}");
            }
        }
    }
}