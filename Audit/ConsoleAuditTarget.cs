using System.Text.Json;

namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Console audit target - long to int conversion hatası düzeltildi
    /// </summary>
    public class ConsoleAuditTarget : IAuditTarget, ISyncAuditTarget, IFlushable
    {
        private readonly ConsoleColor _defaultColor = Console.ForegroundColor;
        private readonly object _lock = new object();
        private readonly JsonSerializerOptions _jsonOptions;

        public ConsoleAuditTarget()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public void Write(AuditLogEntry entry)
        {
            if (entry == null) return;

            lock (_lock)
            {
                try
                {
                    // Set console color based on audit level
                    SetConsoleColor(entry.Level);

                    // Format the log entry
                    var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var level = entry.Level.ToString().ToUpper().PadRight(7);
                    var category = entry.Category.ToString().PadRight(12);

                    // Thread ID conversion fixed - long to int explicit cast
                    var threadId = entry.ThreadId.HasValue ? ((int)entry.ThreadId.Value).ToString() : "N/A";

                    Console.WriteLine($"[{timestamp}] [{level}] [{category}] [T:{threadId}] {entry.Message}");

                    // Print additional data if present
                    if (entry.Data != null)
                    {
                        Console.WriteLine("  Data:");
                        var dataJson = JsonSerializer.Serialize(entry.Data, _jsonOptions);
                        var dataLines = dataJson.Split('\n');
                        foreach (var line in dataLines)
                        {
                            Console.WriteLine($"    {line}");
                        }
                    }

                    // Print exception if present
                    if (!string.IsNullOrEmpty(entry.Exception))
                    {
                        Console.WriteLine("  Exception:");
                        var exceptionLines = entry.Exception.Split('\n');
                        foreach (var line in exceptionLines)
                        {
                            Console.WriteLine($"    {line}");
                        }
                    }

                    Console.WriteLine(); // Empty line for readability
                }
                catch (Exception ex)
                {
                    // Fallback error logging
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[CONSOLE AUDIT ERROR] Failed to write log entry: {ex.Message}");
                    Console.WriteLine($"  Original message: {entry.Message}");
                }
                finally
                {
                    // Reset console color
                    Console.ForegroundColor = _defaultColor;
                }
            }
        }

        public async Task WriteAsync(AuditLogEntry entry)
        {
            await Task.Run(() => Write(entry));
        }

        public void Flush()
        {
            lock (_lock)
            {
                try
                {
                    Console.Out.Flush();
                    Console.Error.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONSOLE AUDIT ERROR] Failed to flush console: {ex.Message}");
                }
            }
        }

        private void SetConsoleColor(AuditLevel level)
        {
            switch (level)
            {
                case AuditLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case AuditLevel.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case AuditLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case AuditLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case AuditLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                default:
                    Console.ForegroundColor = _defaultColor;
                    break;
            }
        }

        // Utility method for compact formatting
        public void WriteCompact(AuditLogEntry entry)
        {
            if (entry == null) return;

            lock (_lock)
            {
                try
                {
                    SetConsoleColor(entry.Level);

                    var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
                    var level = entry.Level.ToString().Substring(0, 1); // Single character
                    var category = entry.Category.ToString().Substring(0, Math.Min(3, entry.Category.ToString().Length)).ToUpper();

                    // Fixed conversion - explicit cast from long to int
                    var threadId = entry.ThreadId.HasValue ? ((int)entry.ThreadId.Value).ToString() : "0";

                    Console.WriteLine($"{timestamp} {level} {category} T{threadId} {entry.Message}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[CONSOLE ERROR] {ex.Message}: {entry.Message}");
                }
                finally
                {
                    Console.ForegroundColor = _defaultColor;
                }
            }
        }

        // Utility method for JSON only output
        public void WriteJsonOnly(AuditLogEntry entry)
        {
            if (entry == null) return;

            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(entry, _jsonOptions);
                    Console.WriteLine(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{{\"error\": \"Failed to serialize audit entry: {ex.Message}\", \"originalMessage\": \"{entry.Message}\"}}");
                }
            }
        }
    }
}