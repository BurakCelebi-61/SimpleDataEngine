using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Static audit logger for application-wide logging
    /// </summary>
    public static class AuditLogger
    {
        private static readonly object _lock = new object();
        private static readonly List<IAuditTarget> _targets = new List<IAuditTarget>();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static AuditLogger()
        {
            // Default file target
            AddTarget(new FileAuditTarget());
        }

        /// <summary>
        /// Add audit target
        /// </summary>
        public static void AddTarget(IAuditTarget target)
        {
            lock (_lock)
            {
                _targets.Add(target);
            }
        }

        /// <summary>
        /// Async log method - eksik olan method
        /// </summary>
        public static async Task LogAsync(string message, object data = null, AuditCategory category = AuditCategory.General)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Information,
                Category = category,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Async error log method - eksik olan method
        /// </summary>
        public static async Task LogErrorAsync(string message, Exception exception = null, object data = null)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Error,
                Category = AuditCategory.Error,
                Message = message,
                Data = data,
                Exception = exception?.ToString(),
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Sync log method - mevcut implementasyon için
        /// </summary>
        public static void Log(string message, object data = null, AuditCategory category = AuditCategory.General)
        {
            LogAsync(message, data, category).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sync error log method - mevcut implementasyon için
        /// </summary>
        public static void LogError(string message, Exception exception = null, object data = null, AuditCategory category = default)
        {
            LogErrorAsync(message, exception, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Warning log method
        /// </summary>
        public static async Task LogWarningAsync(string message, object data = null)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Warning,
                Category = AuditCategory.General,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Debug log method
        /// </summary>
        public static async Task LogDebugAsync(string message, object data = null)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Debug,
                Category = AuditCategory.Debug,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Performance log method
        /// </summary>
        public static async Task LogPerformanceAsync(string operationName, TimeSpan duration, object data = null)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Information,
                Category = AuditCategory.Performance,
                Message = $"Performance: {operationName}",
                Data = new { OperationName = operationName, Duration = duration, AdditionalData = data },
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Security log method
        /// </summary>
        public static async Task LogSecurityAsync(string message, object data = null)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Warning,
                Category = AuditCategory.Security,
                Message = message,
                Data = data,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Database operation log method
        /// </summary>
        public static async Task LogDatabaseAsync(string operation, string entityName, object data = null)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Information,
                Category = AuditCategory.Data,
                Message = $"Database: {operation} on {entityName}",
                Data = new { Operation = operation, EntityName = entityName, AdditionalData = data },
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            await WriteLogEntryAsync(logEntry);
        }

        /// <summary>
        /// Write log entry to all targets
        /// </summary>
        private static async Task WriteLogEntryAsync(AuditLogEntry logEntry)
        {
            var tasks = new List<Task>();

            lock (_lock)
            {
                foreach (var target in _targets)
                {
                    tasks.Add(target.WriteAsync(logEntry));
                }
            }

            if (tasks.Any())
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    // Fallback logging - console'a yaz
                    Console.WriteLine($"[AUDIT ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Failed to write audit log: {ex.Message}");
                    Console.WriteLine($"[AUDIT FALLBACK] {logEntry.Timestamp:yyyy-MM-dd HH:mm:ss} [{logEntry.Level}] {logEntry.Message}");
                }
            }
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public static async Task<AuditStatistics> GetStatisticsAsync()
        {
            var stats = new AuditStatistics();

            foreach (var target in _targets)
            {
                if (target is IStatisticsProvider statsProvider)
                {
                    var targetStats = await statsProvider.GetStatisticsAsync();
                    stats.TotalEntries += targetStats.TotalEntries;
                    stats.ErrorCount += targetStats.ErrorCount;
                    stats.WarningCount += targetStats.WarningCount;
                }
            }

            return stats;
        }

        /// <summary>
        /// Clear all targets
        /// </summary>
        public static void ClearTargets()
        {
            lock (_lock)
            {
                _targets.Clear();
            }
        }

        /// <summary>
        /// Configure file target path
        /// </summary>
        public static void ConfigureFileTarget(string logDirectory)
        {
            lock (_lock)
            {
                // Remove existing file targets
                _targets.RemoveAll(t => t is FileAuditTarget);

                // Add new file target
                _targets.Add(new FileAuditTarget(logDirectory));
            }
        }
    }

    /// <summary>
    /// Audit log entry
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public AuditLevel Level { get; set; }
        public AuditCategory Category { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Exception { get; set; }
        public int ThreadId { get; set; }
        public string MachineName { get; set; }
        public string UserId { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Audit target interface
    /// </summary>
    public interface IAuditTarget
    {
        Task WriteAsync(AuditLogEntry logEntry);
    }

    /// <summary>
    /// Statistics provider interface
    /// </summary>
    public interface IStatisticsProvider
    {
        Task<AuditStatistics> GetStatisticsAsync();
    }

    
}