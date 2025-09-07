using System.Collections.Concurrent;
using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// File-based audit target - long to int conversion hatası düzeltildi
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

            _logDirectory = logDirectory ?? opts.LogDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _fileNameFormat = opts.FileNameFormat;
            _maxFileSizeMB = opts.MaxLogFileSizeMB;
            _maxFiles = opts.MaxLogFiles;
            _compressOldLogs = opts.CompressOldLogs;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Ensure log directory exists
            Directory.CreateDirectory(_logDirectory);

            // Setup flush timer
            _flushTimer = new Timer(FlushCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public void Write(AuditLogEntry entry)
        {
            if (entry == null || _disposed) return;

            try
            {
                var logFilePath = GetCurrentLogFilePath();
                var logLine = FormatLogEntry(entry);

                lock (_lock)
                {
                    File.AppendAllText(logFilePath, logLine + Environment.NewLine);

                    // Check if rotation is needed
                    CheckAndRotateLog(logFilePath);
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if file writing fails
                Console.WriteLine($"[FILE AUDIT ERROR] Failed to write to file: {ex.Message}");
                Console.WriteLine($"  Original entry: {entry.Message}");
            }
        }

        public async Task WriteAsync(AuditLogEntry entry)
        {
            if (entry == null || _disposed) return;

            try
            {
                _pendingEntries.Enqueue(entry);

                // If too many pending entries, flush immediately
                if (_pendingEntries.Count > 100)
                {
                    await FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FILE AUDIT ERROR] Failed to queue entry: {ex.Message}");
            }
        }

        public void Flush()
        {
            if (_disposed) return;

            try
            {