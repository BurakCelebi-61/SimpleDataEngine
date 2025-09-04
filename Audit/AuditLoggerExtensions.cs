namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Extension methods for easier audit logging
    /// </summary>
    public static class AuditLoggerExtensions
    {
        /// <summary>
        /// Logs a data operation
        /// </summary>
        public static void LogDataOperation(this object entity, string operation, object additionalData = null)
        {
            AuditLogger.Log($"DATA_{operation.ToUpper()}", new
            {
                EntityType = entity?.GetType().Name,
                Data = additionalData
            }, category: AuditCategory.Data);
        }

        /// <summary>
        /// Logs a security event
        /// </summary>
        public static void LogSecurityEvent(string eventType, string userId, object data = null)
        {
            AuditLogger.Log(eventType, data, category: AuditCategory.Security);
        }

        /// <summary>
        /// Creates a formatted summary of audit statistics
        /// </summary>
        public static string ToSummaryString(this AuditStatistics stats)
        {
            var summary = new System.Text.StringBuilder();

            summary.AppendLine($"Audit Log Statistics");
            summary.AppendLine($"  Total Entries: {stats.TotalEntries:N0}");
            summary.AppendLine($"  Time Range: {stats.OldestEntry:yyyy-MM-dd} to {stats.NewestEntry:yyyy-MM-dd}");
            summary.AppendLine($"  Duration: {stats.TimeSpan.Days} days");
            summary.AppendLine($"  Avg/Day: {stats.AverageEntriesPerDay:F1}");
            summary.AppendLine($"  Est. Size: {FormatBytes(stats.TotalSizeBytes)}");

            if (stats.CountsByLevel.Any())
            {
                summary.AppendLine();
                summary.AppendLine($"By Level:");
                foreach (var kvp in stats.CountsByLevel.OrderByDescending(x => (int)x.Key))
                {
                    summary.AppendLine($"  {kvp.Key}: {kvp.Value:N0}");
                }
            }

            if (stats.CountsByCategory.Any())
            {
                summary.AppendLine();
                summary.AppendLine($"By Category:");
                foreach (var kvp in stats.CountsByCategory.OrderByDescending(x => x.Value).Take(5))
                {
                    summary.AppendLine($"  {kvp.Key}: {kvp.Value:N0}");
                }
            }

            return summary.ToString().TrimEnd();
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}