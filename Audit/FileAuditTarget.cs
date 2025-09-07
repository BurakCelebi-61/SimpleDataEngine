using System.Collections.Concurrent;
using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// File-based audit target implementation
    /// </summary>
    public class FileAuditTarget : IAuditTarget, ISyncAuditTarget, IFlushable, IAsyncFlushable, IQueryableAuditTarget, IStatisticsProvider, IDisposable
    {
        private readonly string _logDirectory;
        private readonly string _fileNameFormat;
        private readonly int _maxFileSizeMB;
        private readonly int _maxFiles;
        private readonly bool _compressOldLogs;
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<AuditLogEntry> _pendingEntries = new ConcurrentQueue<AuditLogEntry>();
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Timer _flushTimer;
        private bool _disposed = false;

        public FileAuditTarget(string? logDirectory = null, AuditLoggerOptions? options = null)
        {
            var opts = options ?? new AuditLoggerOptions();

            _logDirectory = logDirectory ?? opts.LogDirectory ?? "logs";
            _fileNameFormat = opts.FileNameFormat;
            _maxFileSizeMB = opts.MaxFileSizeMB;
            _maxFiles = opts.MaxFiles;
            _compressOldLogs = opts.CompressOldLogs;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Ensure log directory exists
            Directory.CreateDirectory(_logDirectory);

            // Setup flush timer - Convert TimeSpan to milliseconds (int)
            var flushIntervalMs = (int)opts.FlushInterval.TotalMilliseconds;
            _flushTimer = new Timer(FlushCallback, null, flushIntervalMs, flushIntervalMs);
        }

        public async Task WriteAsync(AuditLogEntry entry)
        {
            if (_disposed) return;

            var logFilePath = GetCurrentLogFilePath();
            var logLine = FormatLogEntry(entry);

            try
            {
                await File.AppendAllTextAsync(logFilePath, logLine + Environment.NewLine);

                // Check if log rotation is needed
                CheckAndRotateLog(logFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to write to log file: {ex.Message}");
            }
        }

        public void Write(AuditLogEntry entry)
        {
            if (_disposed) return;

            _pendingEntries.Enqueue(entry);

            // If we have too many pending entries, force a flush
            if (_pendingEntries.Count > 100)
            {
                FlushInternal();
            }
        }

        public void Flush()
        {
            FlushInternal();
        }

        public async Task FlushAsync()
        {
            await FlushInternalAsync();
        }

        public async Task<AuditQueryResult> QueryAsync(AuditQueryOptions options)
        {
            var startTime = DateTime.UtcNow;
            var entries = new List<AuditLogEntry>();

            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                foreach (var logFile in logFiles)
                {
                    var fileEntries = await ReadLogFileAsync(logFile);
                    entries.AddRange(ApplyFilters(fileEntries, options));

                    // Stop if we have enough entries
                    if (options.Take.HasValue && entries.Count >= options.Take.Value)
                    {
                        entries = entries.Take(options.Take.Value).ToList();
                        break;
                    }
                }

                return new AuditQueryResult
                {
                    Entries = entries,
                    TotalCount = entries.Count,
                    QueryDuration = DateTime.UtcNow - startTime,
                    Success = true
                };
            }
            catch (Exception ex)
            {
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

        public async Task<AuditStatistics> GetStatisticsAsync()
        {
            var stats = new AuditStatistics
            {
                CollectionDate = DateTime.UtcNow,
                EntriesByLevel = new Dictionary<AuditLevel, int>(),
                EntriesByCategory = new Dictionary<AuditCategory, int>()
            };

            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");

                foreach (var logFile in logFiles)
                {
                    var entries = await ReadLogFileAsync(logFile);

                    foreach (var entry in entries)
                    {
                        stats.TotalEntries++;

                        // Count by level
                        if (stats.EntriesByLevel.ContainsKey(entry.Level))
                            stats.EntriesByLevel[entry.Level]++;
                        else
                            stats.EntriesByLevel[entry.Level] = 1;

                        // Count by category
                        if (stats.EntriesByCategory.ContainsKey(entry.Category))
                            stats.EntriesByCategory[entry.Category]++;
                        else
                            stats.EntriesByCategory[entry.Category] = 1;
                    }
                }

                // Calculate average entries per day
                if (stats.TotalEntries > 0)
                {
                    stats.AverageEntriesPerDay = stats.TotalEntries / 30.0; // Estimate based on 30 days
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to get statistics: {ex.Message}");
            }

            return stats;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _flushTimer?.Dispose();
            FlushInternal();
        }

        private string GetCurrentLogFilePath()
        {
            var fileName = string.Format(_fileNameFormat, DateTime.Now);
            return Path.Combine(_logDirectory, fileName);
        }

        private string FormatLogEntry(AuditLogEntry entry)
        {
            return JsonSerializer.Serialize(entry, _jsonOptions);
        }

        private void CheckAndRotateLog(string logFilePath)
        {
            try
            {
                var fileInfo = new FileInfo(logFilePath);
                if (fileInfo.Exists && fileInfo.Length > _maxFileSizeMB * 1024 * 1024)
                {
                    RotateLogFiles();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to check log rotation: {ex.Message}");
            }
        }

        private void RotateLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                // Delete oldest files if we exceed max count
                if (logFiles.Count >= _maxFiles)
                {
                    var filesToDelete = logFiles.Skip(_maxFiles - 1);
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to rotate log files: {ex.Message}");
            }
        }

        private void FlushCallback(object? state)
        {
            FlushInternal();
        }

        private void FlushInternal()
        {
            if (_disposed) return;

            var entriesToFlush = new List<AuditLogEntry>();

            // Dequeue all pending entries
            while (_pendingEntries.TryDequeue(out var entry))
            {
                entriesToFlush.Add(entry);
            }

            if (entriesToFlush.Any())
            {
                try
                {
                    var logFilePath = GetCurrentLogFilePath();
                    var logContent = string.Join(Environment.NewLine,
                        entriesToFlush.Select(FormatLogEntry)) + Environment.NewLine;

                    File.AppendAllText(logFilePath, logContent);
                    CheckAndRotateLog(logFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AUDIT ERROR] Failed to flush entries: {ex.Message}");
                }
            }
        }

        private async Task FlushInternalAsync()
        {
            if (_disposed) return;

            var entriesToFlush = new List<AuditLogEntry>();

            // Dequeue all pending entries
            while (_pendingEntries.TryDequeue(out var entry))
            {
                entriesToFlush.Add(entry);
            }

            if (entriesToFlush.Any())
            {
                try
                {
                    var logFilePath = GetCurrentLogFilePath();
                    var logContent = string.Join(Environment.NewLine,
                        entriesToFlush.Select(FormatLogEntry)) + Environment.NewLine;

                    await File.AppendAllTextAsync(logFilePath, logContent);
                    CheckAndRotateLog(logFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AUDIT ERROR] Failed to flush entries: {ex.Message}");
                }
            }
        }

        private async Task<List<AuditLogEntry>> ReadLogFileAsync(string filePath)
        {
            var entries = new List<AuditLogEntry>();

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var entry = JsonSerializer.Deserialize<AuditLogEntry>(line, _jsonOptions);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                    catch
                    {
                        // Skip malformed lines
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Failed to read log file {filePath}: {ex.Message}");
            }

            return entries;
        }

        private List<AuditLogEntry> ApplyFilters(List<AuditLogEntry> entries, AuditQueryOptions options)
        {
            var filtered = entries.AsEnumerable();

            // Apply date filters
            if (options.StartDate.HasValue)
            {
                filtered = filtered.Where(e => e.Timestamp >= options.StartDate.Value);
            }

            if (options.EndDate.HasValue)
            {
                filtered = filtered.Where(e => e.Timestamp <= options.EndDate.Value);
            }

            // Apply level filter
            if (options.MinLevel.HasValue)
            {
                filtered = filtered.Where(e => e.Level >= options.MinLevel.Value);
            }

            // Apply category filter
            if (options.Categories != null && options.Categories.Length > 0)
            {
                filtered = filtered.Where(e => options.Categories.Contains(e.Category));
            }

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                var searchText = options.SearchText.ToLowerInvariant();
                filtered = filtered.Where(e =>
                    e.Message.ToLowerInvariant().Contains(searchText) ||
                    (e.Data?.ToString()?.ToLowerInvariant().Contains(searchText) ?? false));
            }

            // Apply skip/take
            if (options.Skip.HasValue && options.Skip.Value > 0)
            {
                filtered = filtered.Skip(options.Skip.Value);
            }

            if (options.Take.HasValue && options.Take.Value > 0)
            {
                filtered = filtered.Take(options.Take.Value);
            }

            return filtered.ToList();
        }