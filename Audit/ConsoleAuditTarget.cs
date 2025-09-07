namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Console-based audit target implementation
    /// </summary>
    public class ConsoleAuditTarget : IAuditTarget, ISyncAuditTarget
    {
        private readonly bool _useColors;
        private readonly bool _includeTimestamp;
        private readonly bool _includeCategory;

        public ConsoleAuditTarget(bool useColors = true, bool includeTimestamp = true, bool includeCategory = true)
        {
            _useColors = useColors;
            _includeTimestamp = includeTimestamp;
            _includeCategory = includeCategory;
        }

        public async Task WriteAsync(AuditLogEntry entry)
        {
            await Task.Run(() => Write(entry));
        }

        public void Write(AuditLogEntry entry)
        {
            var message = FormatMessage(entry);

            if (_useColors)
            {
                WriteColoredMessage(entry.Level, message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        private string FormatMessage(AuditLogEntry entry)
        {
            var parts = new List<string>();

            if (_includeTimestamp)
            {
                parts.Add($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}]");
            }

            parts.Add($"[{entry.Level.ToString().ToUpper()}]");

            if (_includeCategory)
            {
                parts.Add($"[{entry.Category}]");
            }

            parts.Add(entry.Message);

            return string.Join(" ", parts);
        }

        private void WriteColoredMessage(AuditLevel level, string message)
        {
            var originalColor = Console.ForegroundColor;

            try
            {
                // Set color based on level
                Console.ForegroundColor = level switch
                {
                    AuditLevel.Debug => ConsoleColor.Gray,
                    AuditLevel.Information => ConsoleColor.White,
                    AuditLevel.Warning => ConsoleColor.Yellow,
                    AuditLevel.Error => ConsoleColor.Red,
                    AuditLevel.Critical => ConsoleColor.Magenta,
                    _ => ConsoleColor.White
                };

                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}