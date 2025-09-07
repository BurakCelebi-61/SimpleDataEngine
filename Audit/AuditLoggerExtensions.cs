using SimpleDataEngine.Performance;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Extension methods for AuditLogger to support missing functionality
    /// </summary>
    public static class AuditLoggerExtensions
    {
        /// <summary>
        /// Logs performance metrics asynchronously
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="metric">Performance metric to log</param>
        /// <param name="category">Audit category</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogPerformanceAsync(this AuditLogger logger, PerformanceMetric metric, AuditCategory category = AuditCategory.Performance)
        {
            var message = $"Operation '{metric.OperationName}' completed in {metric.DurationMs:F2}ms - {(metric.Success ? "Success" : "Failed")}";

            var data = new
            {
                OperationName = metric.OperationName,
                Duration = metric.Duration,
                DurationMs = metric.DurationMs,
                Success = metric.Success,
                Category = metric.Category,
                ThreadId = metric.ThreadId,
                MemoryConsumed = metric.MemoryConsumed,
                RecordCount = metric.RecordCount,
                ErrorMessage = metric.ErrorMessage
            };

            await AuditLogger.LogAsync(message, data, category);

            // Log as error if performance metric indicates failure
            if (!metric.Success && !string.IsNullOrEmpty(metric.ErrorMessage))
            {
                await LogErrorAsync(logger, metric.ErrorMessage, metric.Exception, AuditCategory.Performance);
            }
        }

        /// <summary>
        /// Logs error messages asynchronously
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="message">Error message</param>
        /// <param name="exception">Optional exception details</param>
        /// <param name="category">Audit category</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogErrorAsync(this AuditLogger logger, string message, Exception? exception = null, AuditCategory category = AuditCategory.Error)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Error,
                Category = category,
                Message = message,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName,
                Exception = exception
            };

            if (exception != null)
            {
                logEntry.Data = new
                {
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            await AuditLogger.WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Logs warning messages asynchronously
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="message">Warning message</param>
        /// <param name="data">Optional additional data</param>
        /// <param name="category">Audit category</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogWarningAsync(this AuditLogger logger, string message, object? data = null, AuditCategory category = AuditCategory.General)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Warning,
                Category = category,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await AuditLogger.WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Logs debug messages asynchronously
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="message">Debug message</param>
        /// <param name="data">Optional additional data</param>
        /// <param name="category">Audit category</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogDebugAsync(this AuditLogger logger, string message, object? data = null, AuditCategory category = AuditCategory.Debug)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Debug,
                Category = category,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await AuditLogger.WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Logs information messages asynchronously
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="message">Information message</param>
        /// <param name="data">Optional additional data</param>
        /// <param name="category">Audit category</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogInformationAsync(this AuditLogger logger, string message, object? data = null, AuditCategory category = AuditCategory.General)
        {
            await AuditLogger.LogAsync(message, data, category);
        }

        /// <summary>
        /// Logs operation start
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="data">Optional operation data</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogOperationStartAsync(this AuditLogger logger, string operationName, object? data = null)
        {
            var message = $"Operation '{operationName}' started";
            await AuditLogger.LogAsync(message, data, AuditCategory.Operation);
        }

        /// <summary>
        /// Logs operation completion
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="success">Whether operation was successful</param>
        /// <param name="data">Optional operation data</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogOperationCompleteAsync(this AuditLogger logger, string operationName, TimeSpan duration, bool success = true, object? data = null)
        {
            var message = $"Operation '{operationName}' {(success ? "completed successfully" : "failed")} in {duration.TotalMilliseconds:F2}ms";

            var logData = new
            {
                OperationName = operationName,
                Duration = duration,
                DurationMs = duration.TotalMilliseconds,
                Success = success,
                AdditionalData = data
            };

            var level = success ? AuditLevel.Information : AuditLevel.Error;

            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = AuditCategory.Operation,
                Message = message,
                Data = logData,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await AuditLogger.WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Logs security events
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="securityEvent">Security event description</param>
        /// <param name="userId">User involved in the event</param>
        /// <param name="success">Whether the security operation was successful</param>
        /// <param name="data">Additional security data</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogSecurityEventAsync(this AuditLogger logger, string securityEvent, string? userId = null, bool success = true, object? data = null)
        {
            var level = success ? AuditLevel.Information : AuditLevel.Warning;
            var message = $"Security Event: {securityEvent} - {(success ? "Success" : "Failed")}";

            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = AuditCategory.Security,
                Message = message,
                Data = data,
                UserId = userId,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await AuditLogger.WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Logs bulk operations with performance tracking
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="operationName">Name of the bulk operation</param>
        /// <param name="recordCount">Number of records processed</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="successCount">Number of successful operations</param>
        /// <param name="failureCount">Number of failed operations</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task LogBulkOperationAsync(this AuditLogger logger, string operationName, int recordCount, TimeSpan duration, int successCount, int failureCount)
        {
            var throughput = duration.TotalSeconds > 0 ? recordCount / duration.TotalSeconds : 0;
            var successRate = recordCount > 0 ? (double)successCount / recordCount * 100 : 0;

            var message = $"Bulk operation '{operationName}' processed {recordCount} records in {duration.TotalMilliseconds:F2}ms - Success: {successCount}, Failed: {failureCount} ({successRate:F1}% success rate)";

            var data = new
            {
                OperationName = operationName,
                TotalRecords = recordCount,
                SuccessfulRecords = successCount,
                FailedRecords = failureCount,
                Duration = duration,
                DurationMs = duration.TotalMilliseconds,
                ThroughputPerSecond = throughput,
                SuccessRate = successRate
            };

            var level = failureCount == 0 ? AuditLevel.Information : AuditLevel.Warning;

            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = AuditCategory.Operation,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await AuditLogger.WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Creates a scoped audit operation that automatically logs start and completion
        /// </summary>
        /// <param name="logger">The audit logger instance</param>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="data">Optional operation data</param>
        /// <returns>Disposable audit scope</returns>
        public static async Task<IDisposable> CreateAuditScopeAsync(this AuditLogger logger, string operationName, object? data = null)
        {
            await logger.LogOperationStartAsync(operationName, data);
            return new AuditScope(operationName, data);
        }
    }
