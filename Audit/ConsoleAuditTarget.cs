namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Console-based audit target for debugging and development
    /// </summary>
    public class ConsoleAuditTarget : IAuditTarget, IStatisticsProvider
    {
        private readonly bool _useColors;
        private readonly AuditLevel _minimumLevel;
        private readonly object _writeLock = new object();
        private long _totalEntries;
        private long _errorCount;
        private long _warningCount;
        private DateTime _firstEntry = DateTime.MaxValue;
        private DateTime _lastEntry = DateTime.MinValue;

        public ConsoleAuditTarget(bool useColors = true, AuditLevel minimumLevel = AuditLevel.Information)
        {
            _useColors = useColors;
            _minimumLevel = minimumLevel;
        }

        /// <summary>
        /// Write audit entry to console
        /// </summary>
        public async Task WriteAsync(AuditLogEntry logEntry)
        {
            if (logEntry.Level < _minimumLevel)
                return;

            await Task.Run(() =>
            {
                lock (_writeLock)
                {
                    try
                    {
                        WriteToConsole(logEntry);
                        UpdateStatistics(logEntry);
                    }
                    catch
                    {
                        // Ignore console write errors
                    }
                }
            });
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public async Task<AuditStatistics> GetStatisticsAsync()
        {
            return await Task.FromResult(new AuditStatistics
            {
                TotalEntries = _totalEntries,
                ErrorCount = _errorCount,
                WarningCount = _warningCount,
                FirstEntry = _firstEntry == DateTime.MaxValue ? DateTime.MinValue : _firstEntry,
                LastEntry = _lastEntry
            });
        }

        private void WriteToConsole(AuditLogEntry logEntry)
        {
            var timestamp = logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logEntry.Level.ToString().ToUpper().PadRight(11);
            var category = logEntry.Category.ToString().PadRight(12);

            if (_useColors)
            {
                WriteWithColor(timestamp, ConsoleColor.Gray);
                Console.Write(" ");
                WriteWithColor($"[{level}]", GetLevelColor(logEntry.Level));
                Console.Write(" ");
                WriteWithColor($"[{category}]", ConsoleColor.DarkCyan);
                Console.Write(" ");
                WriteWithColor(logEntry.Message, GetMessageColor(logEntry.Level));

                if (logEntry.Data != null)
                {
                    Console.Write(" ");
                    WriteWithColor($"Data: {System.Text.Json.JsonSerializer.Serialize(logEntry.Data)}", ConsoleColor.DarkGray);
                }

                if (!string.IsNullOrEmpty(logEntry.Exception))
                {
                    Console.WriteLine();
                    WriteWithColor($"Exception: {logEntry.Exception}", ConsoleColor.Red);
                }

                Console.WriteLine();
            }
            else
            {
                var message = $"{timestamp} [{level}] [{category}] {logEntry.Message}";

                if (logEntry.Data != null)
                {
                    message += $" Data: {System.Text.Json.JsonSerializer.Serialize(logEntry.Data)}";
                }

                Console.WriteLine(message);

                if (!string.IsNullOrEmpty(logEntry.Exception))
                {
                    Console.WriteLine($"Exception: {logEntry.Exception}");
                }
            }
        }

        private void WriteWithColor(string text, ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(text);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        private ConsoleColor GetLevelColor(AuditLevel level)
        {
            return level switch
            {
                AuditLevel.Debug => ConsoleColor.DarkGray,
                AuditLevel.Information => ConsoleColor.White,
                AuditLevel.Warning => ConsoleColor.Yellow,
                AuditLevel.Error => ConsoleColor.Red,
                AuditLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }

        private ConsoleColor GetMessageColor(AuditLevel level)
        {
            return level switch
            {
                AuditLevel.Debug => ConsoleColor.DarkGray,
                AuditLevel.Information => ConsoleColor.Gray,
                AuditLevel.Warning => ConsoleColor.Yellow,
                AuditLevel.Error => ConsoleColor.Red,
                AuditLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };
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
    }

    /// <summary>
    /// Database audit target - writes to database table
    /// </summary>
    public class DatabaseAuditTarget : IAuditTarget, IStatisticsProvider
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly SemaphoreSlim _writeLock;
        private long _totalEntries;
        private long _errorCount;
        private long _warningCount;

        public DatabaseAuditTarget(string connectionString, string tableName = "AuditLog")
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _tableName = tableName;
            _writeLock = new SemaphoreSlim(1, 1);
        }

        public async Task WriteAsync(AuditLogEntry logEntry)
        {
            await _writeLock.WaitAsync();
            try
            {
                // Database implementation would go here
                // For now, just track statistics
                UpdateStatistics(logEntry);
                await Task.CompletedTask;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<AuditStatistics> GetStatisticsAsync()
        {
            return await Task.FromResult(new AuditStatistics
            {
                TotalEntries = _totalEntries,
                ErrorCount = _errorCount,
                WarningCount = _warningCount
            });
        }

        private void UpdateStatistics(AuditLogEntry logEntry)
        {
            _totalEntries++;

            if (logEntry.Level == AuditLevel.Error || logEntry.Level == AuditLevel.Critical)
                _errorCount++;
            else if (logEntry.Level == AuditLevel.Warning)
                _warningCount++;
        }
    }

    /// <summary>
    /// Memory audit target - keeps logs in memory (for testing)
    /// </summary>
    public class MemoryAuditTarget : IAuditTarget, IStatisticsProvider
    {
        private readonly List<AuditLogEntry> _entries = new List<AuditLogEntry>();
        private readonly int _maxEntries;
        private readonly object _lock = new object();

        public MemoryAuditTarget(int maxEntries = 1000)
        {
            _maxEntries = maxEntries;
        }

        public IReadOnlyList<AuditLogEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToList();
                }
            }
        }

        public async Task WriteAsync(AuditLogEntry logEntry)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _entries.Add(logEntry);

                    // Keep only the latest entries
                    while (_entries.Count > _maxEntries)
                    {
                        _entries.RemoveAt(0);
                    }
                }
            });
        }

        public async Task<AuditStatistics> GetStatisticsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return new AuditStatistics
                    {
                        TotalEntries = _entries.Count,
                        ErrorCount = _entries.Count(e => e.Level == AuditLevel.Error || e.Level == AuditLevel.Critical),
                        WarningCount = _entries.Count(e => e.Level == AuditLevel.Warning),
                        FirstEntry = _entries.Any() ? _entries.Min(e => e.Timestamp) : DateTime.MinValue,
                        LastEntry = _entries.Any() ? _entries.Max(e => e.Timestamp) : DateTime.MinValue
                    };
                }
            });
        }

        public void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
            }
        }

        public List<AuditLogEntry> Search(string searchTerm)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return _entries.ToList();

                return _entries.Where(e =>
                    e.Message?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    e.Exception?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                ).ToList();
            }
        }

        public List<AuditLogEntry> GetByLevel(AuditLevel level)
        {
            lock (_lock)
            {
                return _entries.Where(e => e.Level == level).ToList();
            }
        }

        public List<AuditLogEntry> GetByCategory(AuditCategory category)
        {
            lock (_lock)
            {
                return _entries.Where(e => e.Category == category).ToList();
            }
        }
    }
}