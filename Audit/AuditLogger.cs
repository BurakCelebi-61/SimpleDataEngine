using System.Collections.Concurrent;
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
        /// Basic log method with proper enum
        /// </summary>
        public static void Log(string message, object? data = null, AuditCategory category = AuditCategory.General)
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
        /// Warning log method - MISSING METHOD ADDED
        /// </summary>
        public static void LogWarning(string message, object? data = null, AuditCategory category = AuditCategory.General)
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
        /// Error log method
        /// </summary>
        public static void LogError(string message, Exception? exception = null, object? data = null, AuditCategory category = AuditCategory.General)
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Error,
                Category = category,
                Message = message,
                Data = data,
                Exception = exception,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            WriteLogEntry(logEntry);
        }

        /// <summary>
        /// Timed log method for performance tracking
        /// </summary>
        public static void LogTimed(string message, TimeSpan duration, object? data = null, AuditCategory category = AuditCategory.Performance)
        {
            var enhancedData = new Dictionary<string, object>
            {
                ["Duration"] = duration.TotalMilliseconds,
                ["DurationFormatted"] = $"{duration.TotalMilliseconds:F2}ms"
            };

            if (data != null)
            {
                enhancedData["Data"] = data;
            }

            var logEntry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = AuditLevel.Information,
                Category = category,
                Message = message,
                Data = enhancedData,
                ThreadId = Environment.CurrentManagedThreadId,
                MachineName = Environment.MachineName
            };

            WriteLogEntry(logEntry);
        }

        /// <summary>
        /// Begin scope method - MISSING METHOD ADDED
        /// </summary>
        public static IDisposable BeginScope(string scopeName, object? data = null)
        {
            return new AuditScope(scopeName, data);
        }

        /// <summary>
        /// Async log method
        /// </summary>
        public static async Task LogAsync(string message, object? data = null, AuditCategory category = AuditCategory.General)
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
        /// Query audit logs - MISSING METHOD ADDED
        /// </summary>
        public static async Task<AuditQueryResult> QueryAsync(AuditQueryOptions options)
        {
            var results = new List<AuditLogEntry>();
            var totalCount = 0;
            var queryStart = DateTime.UtcNow;

            try
            {
                // Query all queryable targets
                var tasks = new List<Task<AuditQueryResult>>();

                lock (_lock)
                {
                    foreach (var target in _targets.OfType<IQueryableAuditTarget>())
                    {
                        tasks.Add(target.QueryAsync(options));
                    }
                }

                if (tasks.Any())
                {
                    var queryResults = await Task.WhenAll(tasks);

                    foreach (var result in queryResults.Where(r => r.Success))
                    {
                        results.AddRange(result.Entries);
                        totalCount += result.TotalCount;
                    }

                    // Apply additional filtering and paging if needed
                    if (options.Take.HasValue)
                    {
                        results = results.Take(options.Take.Value).ToList();
                    }
                }

                return new AuditQueryResult
                {
                    Entries = results,
                    TotalCount = totalCount,
                    QueryDuration = DateTime.UtcNow - queryStart,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new AuditQueryResult
                {
                    Entries = new List<AuditLogEntry>(),
                    TotalCount = 0,
                    QueryDuration = DateTime.UtcNow - queryStart,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Flush all targets - MISSING METHOD ADDED
        /// </summary>
        public static void Flush()
        {
            lock (_lock)
            {
                foreach (var target in _targets.OfType<IFlushable>())
                {
                    target.Flush();
                }
            }
        }

        /// <summary>
        /// Flush all targets asynchronously
        /// </summary>
        public static async Task FlushAsync()
        {
            var tasks = new List<Task>();

            lock (_lock)
            {
                foreach (var target in _targets.OfType<IAsyncFlushable>())
                {
                    tasks.Add(target.FlushAsync());
                }
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public static async Task<AuditStatistics> GetStatisticsAsync()
        {
            var stats = new AuditStatistics
            {
                CollectionDate = DateTime.UtcNow,
                EntriesByLevel = new Dictionary<AuditLevel, int>(),
                EntriesByCategory = new Dictionary<AuditCategory, int>()
            };

            try
            {
                lock (_lock)
                {
                    foreach (var target in _targets.OfType<IStatisticsProvider>())
                    {
                        var targetStats = target.GetStatisticsAsync().Result;

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
}