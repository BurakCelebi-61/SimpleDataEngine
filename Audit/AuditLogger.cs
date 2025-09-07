using System.Collections.Concurrent;
using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Static audit logger for application-wide logging - Eksik metodlar eklendi
    /// </summary>
    public static class AuditLogger
    {
        private static readonly object _lock = new object();
        private static readonly List<IAuditTarget> _targets = new List<IAuditTarget>();
        private static readonly ConcurrentQueue<AuditLogEntry> _logQueue = new ConcurrentQueue<AuditLogEntry>();
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
        /// Remove audit target
        /// </summary>
        public static void RemoveTarget(IAuditTarget target)
        {
            lock (_lock)
            {
                _targets.Remove(target);
            }
        }

        /// <summary>
        /// Basic log method - string category overload for backward compatibility
        /// </summary>
        public static void Log(string eventType, object data = null, string category = "General")
        {
            // Convert string to AuditCategory enum
            if (Enum.TryParse<AuditCategory>(category, true, out var auditCategory))
            {
                Log(eventType, data, auditCategory);
            }
            else
            {
                Log(eventType, data, AuditCategory.General);
            }
        }

        /// <summary>
        /// Basic log method with proper enum
        /// </summary>
        public static void Log(string message, object data = null, AuditCategory category = AuditCategory.General)
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

            WriteLogEntry(logEntry);
        }

        /// <summary>
        /// Async log method
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
        /// Warning log method - EKSIK OLAN METHOD
        /// </summary>
        public static void LogWarning(string message, object data = null, AuditCategory category = AuditCategory.General)
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

            WriteLogEntry(logEntry);
        }

        /// <summary>
        /// Warning log method - string category overload
        /// </summary>
        public static void LogWarning(string eventType, object data, string category)
        {
            if (Enum.TryParse<AuditCategory>(category, true, out var auditCategory))
            {
                LogWarning(eventType, data, auditCategory);
            }
            else
            {
                LogWarning(eventType, data, AuditCategory.General);
            }
        }

        /// <summary>
        /// Error log method
        /// </summary>
        public static void LogError(string message, Exception exception = null, object data = null, AuditCategory category = AuditCategory.Error)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Error,
                Category = category,
                Message = message,
                Data = data,
                Exception = exception?.ToString(),
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            WriteLogEntry(logEntry);
        }

        /// <summary>
        /// Timed operation log method - EKSIK OLAN METHOD
        /// </summary>
        public static void LogTimed(string operationName, TimeSpan duration, object data = null, AuditCategory category = AuditCategory.Performance)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Information,
                Category = category,
                Message = $"Timed Operation: {operationName}",
                Data = new
                {
                    OperationName = operationName,
                    DurationMs = duration.TotalMilliseconds,
                    Duration = duration.ToString(),
                    AdditionalData = data
                },
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            WriteLogEntry(logEntry);
        }

        /// <summary>
        /// Begin scope method - EKSIK OLAN METHOD
        /// </summary>
        public static IDisposable BeginScope(string scopeName, AuditCategory category = AuditCategory.General)
        {
            return new AuditScope(scopeName, category);
        }

        /// <summary>
        /// Query audit logs method - EKSIK OLAN METHOD
        /// </summary>
        public static async Task<AuditQueryResult> QueryAsync(AuditQueryOptions options)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<AuditLogEntry>();
            var totalCount = 0;

            try
            {
                // Query from all targets that support querying
                foreach (var target in _targets)
                {
                    if (target is IQueryableAuditTarget queryableTarget)
                    {
                        var targetResults = await queryableTarget.QueryAsync(options);
                        results.AddRange(targetResults.Entries);
                        totalCount += targetResults.TotalCount;
                    }
                }

                // Apply sorting and pagination if needed
                var orderedResults = results
                    .OrderByDescending(x => x.Timestamp)
                    .Take(options.Take ?? 100)
                    .ToList();

                return new AuditQueryResult
                {
                    Entries = orderedResults,
                    TotalCount = totalCount,
                    QueryDuration = DateTime.UtcNow - startTime,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                LogError("Query operation failed", ex, new { Options = options });

                return new AuditQueryResult
                {
                    Entries = new List<AuditLogEntry>(),
                    TotalCount = 0,
                    QueryDuration = DateTime.UtcNow - startTime,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Flush all pending logs - EKSIK OLAN METHOD
        /// </summary>
        public static void Flush()
        {
            try
            {
                // Process any queued entries
                while (_logQueue.TryDequeue(out var entry))
                {
                    WriteLogEntry(entry);
                }

                // Flush all targets
                foreach (var target in _targets)
                {
                    if (target is IFlushable flushableTarget)
                    {
                        flushableTarget.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to flush logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Async flush method
        /// </summary>
        public static async Task FlushAsync()
        {
            try
            {
                // Process any queued entries
                while (_logQueue.TryDequeue(out var entry))
                {
                    await WriteLogEntryAsync(entry);
                }

                // Flush all targets
                var flushTasks = _targets
                    .OfType<IAsyncFlushable>()
                    .Select(target => target.FlushAsync())
                    .ToArray();

                if (flushTasks.Any())
                {
                    await Task.WhenAll(flushTasks);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to flush logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Get audit statistics - EKSIK OLAN METHOD
        /// </summary>
        public static async Task<AuditStatistics> GetStatisticsAsync()
        {
            var stats = new AuditStatistics
            {
                TotalEntries = 0,
                EntriesByLevel = new Dictionary<AuditLevel, int>(),
                EntriesByCategory = new Dictionary<AuditCategory, int>(),
                AverageEntriesPerDay = 0,
                CollectionDate = DateTime.UtcNow
            };

            try
            {
                foreach (var target in _targets)
                {
                    if (target is IStatisticsProvider statsProvider)
                    {
                        var targetStats = await statsProvider.GetStatisticsAsync();
                        stats.TotalEntries += targetStats.TotalEntries;

                        // Merge level statistics
                        foreach (var kvp in targetStats.EntriesByLevel)
                        {
                            if (stats.EntriesByLevel.ContainsKey(kvp.Key))
                                stats.EntriesByLevel[kvp.Key] += kvp.Value;
                            else
                                stats.EntriesByLevel[kvp.Key] = kvp.Value;
                        }

                        // Merge category statistics
                        foreach (var kvp in targetStats.EntriesByCategory)
                        {
                            if (stats.EntriesByCategory.ContainsKey(kvp.Key))
                                stats.EntriesByCategory[kvp.Key] += kvp.Value;
                            else
                                stats.EntriesByCategory[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Calculate average entries per day
                if (stats.TotalEntries > 0)
                {
                    // Assume data collection started 30 days ago for estimation
                    stats.AverageEntriesPerDay = stats.TotalEntries / 30.0;
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to get audit statistics", ex);
            }

            return stats;
        }

        /// <summary>
        /// Synchronous write log entry
        /// </summary>
        private static void WriteLogEntry(AuditLogEntry logEntry)
        {
            try
            {
                lock (_lock)
                {
                    foreach (var target in _targets)
                    {
                        try
                        {
                            if (target is ISyncAuditTarget syncTarget)
                            {
                                syncTarget.Write(logEntry);
                            }
                            else
                            {
                                // Queue for async processing
                                _logQueue.Enqueue(logEntry);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[AUDIT ERROR] Failed to write to target {target.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Critical error in WriteLogEntry: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronous write log entry
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
                    // Fallback logging
                    Console.WriteLine($"[AUDIT ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Failed to write audit log: {ex.Message}");
                    Console.WriteLine($"[AUDIT FALLBACK] {logEntry.Timestamp:yyyy-MM-dd HH:mm:ss} [{logEntry.Level}] {logEntry.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Supporting interfaces for audit functionality
    /// </summary>
    public interface ISyncAuditTarget
    {
        void Write(AuditLogEntry entry);
    }

    public interface IFlushable
    {
        void Flush();
    }

    public interface IAsyncFlushable
    {
        Task FlushAsync();
    }

    public interface IQueryableAuditTarget
    {
        Task<AuditQueryResult> QueryAsync(AuditQueryOptions options);
    }

    public interface IStatisticsProvider
    {
        Task<AuditStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Audit query options
    /// </summary>
    public class AuditQueryOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public AuditLevel? MinLevel { get; set; }
        public AuditCategory[]? Categories { get; set; }
        public string? SearchText { get; set; }
        public int? Take { get; set; } = 100;
        public int? Skip { get; set; } = 0;
    }

    /// <summary>
    /// Audit query result
    /// </summary>
    public class AuditQueryResult
    {
        public List<AuditLogEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public TimeSpan QueryDuration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Audit statistics
    /// </summary>
    public class AuditStatistics
    {
        public int TotalEntries { get; set; }
        public Dictionary<AuditLevel, int> EntriesByLevel { get; set; } = new();
        public Dictionary<AuditCategory, int> EntriesByCategory { get; set; } = new();
        public double AverageEntriesPerDay { get; set; }
        public DateTime CollectionDate { get; set; }
    }
}