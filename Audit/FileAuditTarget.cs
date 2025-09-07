using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// File-based audit target implementation
    /// </summary>
    public class FileAuditTarget : IAuditTarget, IStatisticsProvider, IDisposable
    {
        private readonly string _logDirectory;
        private readonly string _logFilePattern;
        private readonly SemaphoreSlim _writeLock;
        private readonly JsonSerializerOptions _jsonOptions;
        private long _totalEntries;
        private long _errorCount;
        private long _warningCount;
        private DateTime _firstEntry = DateTime.MaxValue;
        private DateTime _lastEntry = DateTime.MinValue;
        private bool _disposed;

        public FileAuditTarget(string logDirectory = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _logFilePattern = "audit_{0:yyyy-MM-dd}.log";
            _writeLock = new SemaphoreSlim(1, 1);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            EnsureLogDirectoryExists();
        }

        /// <summary>
        /// Write audit entry to file
        /// </summary>
        public async Task WriteAsync(AuditLogEntry logEntry)
        {
            if (_disposed) return;

            await _writeLock.WaitAsync();
            try
            {
                var logFile = GetLogFilePath(logEntry.Timestamp);
                var logLine = FormatLogEntry(logEntry);

                await File.AppendAllTextAsync(logFile, logLine + Environment.NewLine);

                UpdateStatistics(logEntry);
            }
            catch (Exception ex)
            {
                // Fallback - try to write to console
                Console.WriteLine($"[AUDIT FILE ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Failed to write to log file: {ex.Message}");
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public async Task<AuditStatistics> GetStatisticsAsync()
        {
            await _writeLock.WaitAsync();
            try
            {
                return new AuditStatistics
                {
                    TotalEntries = _totalEntries,
                    ErrorCount = _errorCount,
                    WarningCount = _warningCount,
                    FirstEntry = _firstEntry == DateTime.MaxValue ? DateTime.MinValue : _firstEntry,
                    LastEntry = _lastEntry
                };
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Read log entries from file
        /// </summary>
        public async Task<List<AuditLogEntry>> ReadLogEntriesAsync(DateTime date)
        {
            var logFile = GetLogFilePath(date);
            if (!File.Exists(logFile))
                return new List<AuditLogEntry>();

            var entries = new List<AuditLogEntry>();
            var lines = await File.ReadAllLinesAsync(logFile);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var entry = ParseLogEntry(line);
                    if (entry != null)
                        entries.Add(entry);
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            return entries;
        }

        /// <summary>
        /// Read log entries within date range
        /// </summary>
        public async Task<List<AuditLogEntry>> ReadLogEntriesAsync(DateTime fromDate, DateTime toDate)
        {
            var allEntries = new List<AuditLogEntry>();
            var currentDate = fromDate.Date;

            while (currentDate <= toDate.Date)
            {
                var dayEntries = await ReadLogEntriesAsync(currentDate);
                allEntries.AddRange(dayEntries.Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate));
                currentDate = currentDate.AddDays(1);
            }

            return allEntries.OrderBy(e => e.Timestamp).ToList();
        }

        /// <summary>
        /// Search log entries by message content
        /// </summary>
        public async Task<List<AuditLogEntry>> SearchLogEntriesAsync(string searchTerm, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.Today.AddDays(-7);
            var endDate = toDate ?? DateTime.Today.AddDays(1);

            var entries = await ReadLogEntriesAsync(startDate, endDate);

            if (string.IsNullOrWhiteSpace(searchTerm))
                return entries;

            return entries.Where(e =>
                e.Message?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                e.Exception?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();
        }

        /// <summary>
        /// Get log files in directory
        /// </summary>
        public async Task<List<string>> GetLogFilesAsync()
        {
            if (!Directory.Exists(_logDirectory))
                return new List<string>();

            return await Task.Run(() =>
                Directory.GetFiles(_logDirectory, "audit_*.log")
                         .OrderByDescending(f => f)
                         .ToList()
            );
        }

        /// <summary>
        /// Clean up old log files
        /// </summary>
        public async Task CleanupOldLogsAsync(int retentionDays = 30)
        {
            if (!Directory.Exists(_logDirectory))
                return;

            var cutoffDate = DateTime.Today.AddDays(-retentionDays);

            var oldFiles = Directory.GetFiles(_logDirectory, "audit_*.log")
                .Where(f => File.GetCreationTime(f).Date < cutoffDate)
                .ToList();

            foreach (var file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }

        /// <summary>
        /// Archive log files to compressed format
        /// </summary>
        public async Task ArchiveLogsAsync(DateTime beforeDate)
        {
            // Implementation for archiving old logs
            // This could compress old logs or move them to archive directory
            await Task.CompletedTask; // Placeholder
        }

        private void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string GetLogFilePath(DateTime timestamp)
        {
            var fileName = string.Format(_logFilePattern, timestamp);
            return Path.Combine(_logDirectory, fileName);
        }

        private string FormatLogEntry(AuditLogEntry logEntry)
        {
            var logData = new
            {
                timestamp = logEntry.Timestamp,
                level = logEntry.Level.ToString(),
                category = logEntry.Category.ToString(),
                message = logEntry.Message,
                data = logEntry.Data,
                exception = logEntry.Exception,
                threadId = logEntry.ThreadId,
                machineName = logEntry.MachineName,
                userId = logEntry.UserId,
                correlationId = logEntry.CorrelationId
            };

            return JsonSerializer.Serialize(logData, _jsonOptions);
        }

        private AuditLogEntry ParseLogEntry(string logLine)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(logLine);
                var root = jsonDoc.RootElement;

                return new AuditLogEntry
                {
                    Timestamp = root.GetProperty("timestamp").GetDateTime(),
                    Level = Enum.Parse<AuditLevel>(root.GetProperty("level").GetString()),
                    Category = Enum.Parse<AuditCategory>(root.GetProperty("category").GetString()),
                    Message = root.GetProperty("message").GetString(),
                    Data = root.TryGetProperty("data", out var dataElement) ? dataElement.GetRawText() : null,
                    Exception = root.TryGetProperty("exception", out var exElement) ? exElement.GetString() : null,
                    ThreadId = root.GetProperty("threadId").GetInt32(),
                    MachineName = root.TryGetProperty("machineName", out var machineElement) ? machineElement.GetString() : null,
                    UserId = root.TryGetProperty("userId", out var userElement) ? userElement.GetString() : null,
                    CorrelationId = root.TryGetProperty("correlationId", out var corrElement) ? corrElement.GetString() : null
                };
            }
            catch
            {
                return null;
            }
        }

        private void UpdateStatistics(AuditLogEntry logEntry)
        {
            _totalEntries++;

            if (logEntry.Level == AuditLevel.Error || logEntry.Level == AuditLevel.Critical)
                _errorCount++;
            else if (logEntry.Level == AuditLevel.Warning)
                _warningCount++;

            if (logEntry.Timestamp < _firstEntry)
                _firstEntry = logEntry.Timestamp;

            if (logEntry.Timestamp > _lastEntry)
                _lastEntry = logEntry.Timestamp;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _writeLock?.Dispose();
                _disposed = true;
            }
        }
    }
}