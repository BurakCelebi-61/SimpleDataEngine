using SimpleDataEngine.Configuration;
using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Comprehensive audit logging system for SimpleDataEngine
    /// </summary>
    public static class AuditLogger
    {
        private static readonly object _lock = new object();
        private static readonly List<AuditEntry> _buffer = new();
        private static Timer _flushTimer;
        private static string _currentLogFile;
        private static AuditLoggerOptions _options;
        private static bool _initialized = false;

        static AuditLogger()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the audit logger with default or configured options
        /// </summary>
        public static void Initialize(AuditLoggerOptions options = null)
        {
            lock (_lock)
            {
                if (_initialized && options == null)
                    return;

                _options = options ?? GetDefaultOptions();
                _initialized = true;

                // Setup flush timer for async logging
                if (_options.AsyncLogging)
                {
                    _flushTimer?.Dispose();
                    _flushTimer = new Timer(FlushBuffer, null, _options.FlushInterval, _options.FlushInterval);
                }

                // Ensure log directory exists
                if (!Directory.Exists(_options.LogDirectory))
                {
                    Directory.CreateDirectory(_options.LogDirectory);
                }

                // Cleanup old logs if auto cleanup is enabled
                if (_options.AutoCleanup)
                {
                    Task.Run(CleanupOldLogs);
                }
            }
        }

        /// <summary>
        /// Logs an informational event
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="data">Event data</param>
        /// <param name="message">Optional message</param>
        /// <param name="category">Event category</param>
        public static void Log(string eventType, object data = null, string message = null, AuditCategory category = AuditCategory.System)
        {
            LogEntry(AuditLevel.Info, category, eventType, message, data);
        }

        /// <summary>
        /// Logs a warning event
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="data">Event data</param>
        /// <param name="message">Optional message</param>
        /// <param name="category">Event category</param>
        public static void LogWarning(string eventType, object data = null, string message = null, AuditCategory category = AuditCategory.System)
        {
            LogEntry(AuditLevel.Warning, category, eventType, message, data);
        }

        /// <summary>
        /// Logs an error event
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="data">Additional event data</param>
        /// <param name="category">Event category</param>
        public static void LogError(string eventType, Exception exception, object data = null, AuditCategory category = AuditCategory.System)
        {
            LogEntry(AuditLevel.Error, category, eventType, exception?.Message, data, exception);
        }

        /// <summary>
        /// Logs a critical event
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="data">Event data</param>
        /// <param name="message">Optional message</param>
        /// <param name="category">Event category</param>
        public static void LogCritical(string eventType, object data = null, string message = null, AuditCategory category = AuditCategory.System)
        {
            LogEntry(AuditLevel.Critical, category, eventType, message, data);
        }

        /// <summary>
        /// Logs a debug event
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="data">Event data</param>
        /// <param name="message">Optional message</param>
        /// <param name="category">Event category</param>
        public static void LogDebug(string eventType, object data = null, string message = null, AuditCategory category = AuditCategory.System)
        {
            LogEntry(AuditLevel.Debug, category, eventType, message, data);
        }

        /// <summary>
        /// Logs a timed operation
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="data">Event data</param>
        /// <param name="category">Event category</param>
        public static void LogTimed(string eventType, TimeSpan duration, object data = null, AuditCategory category = AuditCategory.Performance)
        {
            var entry = CreateEntry(AuditLevel.Info, category, eventType, null, data);
            entry.Duration = duration;
            WriteEntry(entry);
        }

        /// <summary>
        /// Creates a scoped audit context for tracking operations
        /// </summary>
        /// <param name="eventType">Event type identifier</param>
        /// <param name="category">Event category</param>
        /// <returns>Disposable audit scope</returns>
        public static IDisposable BeginScope(string eventType, AuditCategory category = AuditCategory.System)
        {
            return new AuditScope(eventType, category);
        }

        /// <summary>
        /// Forces immediate flush of buffered log entries
        /// </summary>
        public static void Flush()
        {
            lock (_lock)
            {
                FlushBuffer(null);
            }
        }

        /// <summary>
        /// Queries audit logs with specified criteria
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>Query result with matching entries</returns>
        public static async Task<AuditQueryResult> QueryAsync(AuditQueryOptions options)
        {
            var startTime = DateTime.Now;
            var result = new AuditQueryResult
            {
                Skip = options.Skip,
                Take = options.Take
            };

            try
            {
                var allEntries = await LoadEntriesInDateRange(options.StartDate, options.EndDate);

                // Apply filters
                var filteredEntries = allEntries.AsQueryable();

                if (options.Levels?.Any() == true)
                    filteredEntries = filteredEntries.Where(e => options.Levels.Contains(e.Level));

                if (options.Categories?.Any() == true)
                    filteredEntries = filteredEntries.Where(e => options.Categories.Contains(e.Category));

                if (options.EventTypes?.Any() == true)
                    filteredEntries = filteredEntries.Where(e => options.EventTypes.Contains(e.EventType));

                if (!string.IsNullOrWhiteSpace(options.UserId))
                    filteredEntries = filteredEntries.Where(e => e.UserId == options.UserId);

                if (!string.IsNullOrWhiteSpace(options.SessionId))
                    filteredEntries = filteredEntries.Where(e => e.SessionId == options.SessionId);

                if (!string.IsNullOrWhiteSpace(options.SearchText))
                {
                    var searchLower = options.SearchText.ToLower();
                    filteredEntries = filteredEntries.Where(e =>
                        (e.Message != null && e.Message.ToLower().Contains(searchLower)) ||
                        (e.EventType != null && e.EventType.ToLower().Contains(searchLower)));
                }

                if (!options.IncludeExceptions)
                    filteredEntries = filteredEntries.Where(e => e.Exception == null);

                // Apply sorting
                if (options.SortDescending)
                    filteredEntries = filteredEntries.OrderByDescending(e => e.Timestamp);
                else
                    filteredEntries = filteredEntries.OrderBy(e => e.Timestamp);

                // Get total count before paging
                result.TotalCount = filteredEntries.Count();

                // Apply paging
                result.Entries = filteredEntries.Skip(options.Skip).Take(options.Take).ToList();
                result.QueryDuration = DateTime.Now - startTime;

                return result;
            }
            catch (Exception ex)
            {
                LogError("AUDIT_QUERY_FAILED", ex);
                result.QueryDuration = DateTime.Now - startTime;
                return result;
            }
        }

        /// <summary>
        /// Gets audit statistics for the specified date range
        /// </summary>
        /// <param name="startDate">Start date (null = all time)</param>
        /// <param name="endDate">End date (null = now)</param>
        /// <returns>Audit statistics</returns>
        public static async Task<AuditStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var entries = await LoadEntriesInDateRange(startDate, endDate);

                if (!entries.Any())
                {
                    return new AuditStatistics();
                }

                var stats = new AuditStatistics
                {
                    TotalEntries = entries.Count,
                    OldestEntry = entries.Min(e => e.Timestamp),
                    NewestEntry = entries.Max(e => e.Timestamp)
                };

                // Group by level
                stats.CountsByLevel = entries
                    .GroupBy(e => e.Level)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Group by category
                stats.CountsByCategory = entries
                    .GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Group by event type (top 20)
                stats.CountsByEventType = entries
                    .GroupBy(e => e.EventType)
                    .OrderByDescending(g => g.Count())
                    .Take(20)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate approximate size
                var sampleEntry = JsonSerializer.Serialize(entries.First());
                stats.TotalSizeBytes = sampleEntry.Length * entries.Count;

                return stats;
            }
            catch (Exception ex)
            {
                LogError("AUDIT_STATISTICS_FAILED", ex);
                return new AuditStatistics();
            }
        }

        /// <summary>
        /// Exports audit logs to a file
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <param name="options">Query options for filtering</param>
        /// <returns>Number of exported entries</returns>
        public static async Task<int> ExportAsync(string outputPath, AuditQueryOptions options = null)
        {
            try
            {
                options ??= new AuditQueryOptions { Take = int.MaxValue };
                options.Take = int.MaxValue; // Export all matching entries

                var result = await QueryAsync(options);

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var exportData = new
                {
                    ExportedAt = DateTime.Now,
                    TotalEntries = result.TotalCount,
                    Criteria = options,
                    result.Entries
                };

                var json = JsonSerializer.Serialize(exportData, jsonOptions);
                await File.WriteAllTextAsync(outputPath, json);

                Log("AUDIT_EXPORTED", new { OutputPath = outputPath, EntryCount = result.TotalCount });
                return result.TotalCount;
            }
            catch (Exception ex)
            {
                LogError("AUDIT_EXPORT_FAILED", ex, new { OutputPath = outputPath });
                return 0;
            }
        }

        /// <summary>
        /// Clears all audit logs (use with caution)
        /// </summary>
        /// <param name="olderThan">Only clear logs older than this date (null = all)</param>
        public static void ClearLogs(DateTime? olderThan = null)
        {
            lock (_lock)
            {
                try
                {
                    var logFiles = Directory.GetFiles(_options.LogDirectory, "audit_*.json");
                    int deletedFiles = 0;

                    foreach (var file in logFiles)
                    {
                        var fileInfo = new FileInfo(file);

                        if (olderThan == null || fileInfo.CreationTime < olderThan)
                        {
                            File.Delete(file);
                            deletedFiles++;
                        }
                    }

                    Log("AUDIT_LOGS_CLEARED", new { DeletedFiles = deletedFiles, OlderThan = olderThan });
                }
                catch (Exception ex)
                {
                    LogError("AUDIT_CLEAR_FAILED", ex);
                }
            }
        }

        #region Private Methods

        private static void LogEntry(AuditLevel level, AuditCategory category, string eventType, string message, object data, Exception exception = null)
        {
            if (!_options.Enabled || level < _options.MinimumLevel)
                return;

            var entry = CreateEntry(level, category, eventType, message, data, exception);
            WriteEntry(entry);
        }

        private static AuditEntry CreateEntry(AuditLevel level, AuditCategory category, string eventType, string message, object data, Exception exception = null)
        {
            var entry = new AuditEntry
            {
                Level = level,
                Category = category,
                EventType = eventType,
                Message = message,
                Data = data,
                Exception = exception,
                Source = GetCallingMethod()
            };

            if (_options.IncludeStackTrace && (level == AuditLevel.Error || level == AuditLevel.Critical))
            {
                entry.StackTrace = Environment.StackTrace;
            }

            // Add system context if enabled
            if (_options.IncludeSystemInfo)
            {
                entry.Properties["ProcessId"] = Environment.ProcessId;
                entry.Properties["ThreadId"] = Thread.CurrentThread.ManagedThreadId;
                entry.Properties["WorkingSet"] = Environment.WorkingSet;
            }

            return entry;
        }

        private static void WriteEntry(AuditEntry entry)
        {
            if (_options.AsyncLogging)
            {
                lock (_lock)
                {
                    _buffer.Add(entry);

                    // Force flush if buffer is full
                    if (_buffer.Count >= _options.BufferSize)
                    {
                        FlushBuffer(null);
                    }
                }
            }
            else
            {
                WriteEntryToFile(entry);
            }
        }

        private static void FlushBuffer(object state)
        {
            List<AuditEntry> entriesToFlush;

            lock (_lock)
            {
                if (!_buffer.Any())
                    return;

                entriesToFlush = new List<AuditEntry>(_buffer);
                _buffer.Clear();
            }

            foreach (var entry in entriesToFlush)
            {
                WriteEntryToFile(entry);
            }
        }

        private static void WriteEntryToFile(AuditEntry entry)
        {
            try
            {
                var logFile = GetCurrentLogFile();
                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.AppendAllText(logFile, json + Environment.NewLine);

                // Check if we need to rotate log file
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.Length > _options.MaxLogFileSizeMB * 1024 * 1024)
                {
                    RotateLogFile();
                }
            }
            catch (Exception ex)
            {
                // Can't log this error using the audit logger, so write to console or event log
                Console.WriteLine($"Failed to write audit log: {ex.Message}");
            }
        }

        private static string GetCurrentLogFile()
        {
            if (string.IsNullOrEmpty(_currentLogFile) || !File.Exists(_currentLogFile))
            {
                var fileName = string.Format(_options.FileNameFormat, DateTime.Now);
                _currentLogFile = Path.Combine(_options.LogDirectory, fileName);
            }

            return _currentLogFile;
        }

        private static void RotateLogFile()
        {
            _currentLogFile = null; // Force creation of new log file

            if (_options.AutoCleanup)
            {
                Task.Run(CleanupOldLogs);
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var logFiles = Directory.GetFiles(_options.LogDirectory, "audit_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Remove excess files
                if (logFiles.Count > _options.MaxLogFiles)
                {
                    var filesToDelete = logFiles.Skip(_options.MaxLogFiles);
                    foreach (var file in filesToDelete)
                    {
                        file.Delete();
                    }
                }

                // Compress old logs if enabled
                if (_options.CompressOldLogs)
                {
                    var oldFiles = logFiles
                        .Where(f => f.CreationTime < DateTime.Now.AddDays(-7))
                        .Where(f => !f.Name.EndsWith(".gz"))
                        .Take(10); // Limit to avoid long-running operations

                    foreach (var file in oldFiles)
                    {
                        CompressLogFile(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup audit logs: {ex.Message}");
            }
        }

        private static void CompressLogFile(string filePath)
        {
            try
            {
                var compressedPath = filePath + ".gz";

                using var originalStream = new FileStream(filePath, FileMode.Open);
                using var compressedStream = new FileStream(compressedPath, FileMode.Create);
                using var gzipStream = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Compress);

                originalStream.CopyTo(gzipStream);

                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to compress log file {filePath}: {ex.Message}");
            }
        }

        private static async Task<List<AuditEntry>> LoadEntriesInDateRange(DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<AuditEntry>();

            try
            {
                var logFiles = Directory.GetFiles(_options.LogDirectory, "audit_*.json");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);

                    // Skip files outside date range based on file creation time
                    if (startDate.HasValue && fileInfo.CreationTime.Date < startDate.Value.Date)
                        continue;
                    if (endDate.HasValue && fileInfo.CreationTime.Date > endDate.Value.Date)
                        continue;

                    var lines = await File.ReadAllLinesAsync(file);

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        try
                        {
                            var entry = JsonSerializer.Deserialize<AuditEntry>(line, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            if (entry == null) continue;

                            // Apply date range filter on actual entry timestamp
                            if (startDate.HasValue && entry.Timestamp < startDate.Value)
                                continue;
                            if (endDate.HasValue && entry.Timestamp > endDate.Value)
                                continue;

                            entries.Add(entry);
                        }
                        catch (JsonException)
                        {
                            // Skip malformed entries
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load audit entries: {ex.Message}");
            }

            return entries;
        }

        private static string GetCallingMethod()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                var frame = stackTrace.GetFrame(3); // Skip this method, WriteEntry, LogEntry
                return $"{frame.GetMethod().DeclaringType.Name}.{frame.GetMethod().Name}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static AuditLoggerOptions GetDefaultOptions()
        {
            var dataDirectory = ConfigManager.Current?.Database?.DataDirectory ??
                               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleDataEngine");

            return new AuditLoggerOptions
            {
                LogDirectory = Path.Combine(dataDirectory, "Logs")
            };
        }

        #endregion

        /// <summary>
        /// Disposes resources used by the audit logger
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                _flushTimer?.Dispose();
                FlushBuffer(null);
            }
        }
    }
}