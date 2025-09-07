namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Extension methods for NotificationSeverity
    /// </summary>
    public static class NotificationSeverityExtensions
    {
        /// <summary>
        /// Gets the display name for the severity
        /// </summary>
        public static string GetDisplayName(this NotificationSeverity severity)
        {
            return severity switch
            {
                NotificationSeverity.Information => "Info",
                NotificationSeverity.Debug => "Debug",
                NotificationSeverity.Success => "Success",
                NotificationSeverity.Warning => "Warning",
                NotificationSeverity.Error => "Error",
                NotificationSeverity.Critical => "Critical",
                NotificationSeverity.Fatal => "Fatal",
                _ => severity.ToString()
            };
        }

        /// <summary>
        /// Gets the color associated with the severity
        /// </summary>
        public static ConsoleColor GetConsoleColor(this NotificationSeverity severity)
        {
            return severity switch
            {
                NotificationSeverity.Information => ConsoleColor.White,
                NotificationSeverity.Debug => ConsoleColor.Gray,
                NotificationSeverity.Success => ConsoleColor.Green,
                NotificationSeverity.Warning => ConsoleColor.Yellow,
                NotificationSeverity.Error => ConsoleColor.Red,
                NotificationSeverity.Critical => ConsoleColor.Magenta,
                NotificationSeverity.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Checks if this severity should trigger immediate attention
        /// </summary>
        public static bool RequiresImmediateAttention(this NotificationSeverity severity)
        {
            return severity >= NotificationSeverity.Error;
        }

        /// <summary>
        /// Gets numeric priority (higher number = higher priority)
        /// </summary>
        public static int GetPriority(this NotificationSeverity severity)
        {
            return (int)severity;
        }

        /// <summary>
        /// Converts from string to NotificationSeverity
        /// </summary>
        public static NotificationSeverity Parse(string severityString)
        {
            if (Enum.TryParse<NotificationSeverity>(severityString, true, out var result))
            {
                return result;
            }

            // Handle common variations
            return severityString.ToLowerInvariant() switch
            {
                "info" => NotificationSeverity.Information,
                "warn" => NotificationSeverity.Warning,
                "err" => NotificationSeverity.Error,
                "crit" => NotificationSeverity.Critical,
                _ => NotificationSeverity.Information
            };
        }
    }
}