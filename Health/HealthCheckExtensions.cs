namespace SimpleDataEngine.Health
{
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Converts health report to a formatted string
        /// </summary>
        /// <param name="report">Health report</param>
        /// <returns>Formatted health report</returns>
        public static string ToSummaryString(this HealthReport report)
        {
            var summary = new System.Text.StringBuilder();

            var statusIcon = report.OverallStatus switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Warning => "⚠️",
                HealthStatus.Unhealthy => "❌",
                HealthStatus.Critical => "🚨",
                _ => "❓"
            };

            summary.AppendLine($"{statusIcon} Overall Status: {report.OverallStatus}");
            summary.AppendLine($"🔍 Checks: {report.Checks.Count} total");

            if (report.HealthyCount > 0)
                summary.AppendLine($"  ✅ Healthy: {report.HealthyCount}");
            if (report.WarningCount > 0)
                summary.AppendLine($"  ⚠️ Warnings: {report.WarningCount}");
            if (report.UnhealthyCount > 0)
                summary.AppendLine($"  ❌ Unhealthy: {report.UnhealthyCount}");
            if (report.CriticalCount > 0)
                summary.AppendLine($"  🚨 Critical: {report.CriticalCount}");

            summary.AppendLine($"⏱️ Duration: {report.TotalDuration.TotalMilliseconds:F0}ms");
            summary.AppendLine($"📅 Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");

            // Show details for non-healthy checks
            var problemChecks = report.Checks.Where(c => c.Status != HealthStatus.Healthy).ToList();
            if (problemChecks.Any())
            {
                summary.AppendLine();
                summary.AppendLine("📋 Issues:");

                foreach (var check in problemChecks.Take(10)) // Limit to first 10
                {
                    var checkIcon = check.Status switch
                    {
                        HealthStatus.Warning => "⚠️",
                        HealthStatus.Unhealthy => "❌",
                        HealthStatus.Critical => "🚨",
                        _ => "❓"
                    };

                    summary.AppendLine($"  {checkIcon} {check.Name}: {check.Message}");

                    if (!string.IsNullOrWhiteSpace(check.Recommendation))
                    {
                        summary.AppendLine($"    💡 {check.Recommendation}");
                    }
                }

                if (problemChecks.Count > 10)
                {
                    summary.AppendLine($"  ... and {problemChecks.Count - 10} more issues");
                }
            }

            return summary.ToString().TrimEnd();
        }

        /// <summary>
        /// Gets a simple health status string
        /// </summary>
        /// <param name="report">Health report</param>
        /// <returns>Simple status string</returns>
        public static string ToStatusString(this HealthReport report)
        {
            return report.OverallStatus switch
            {
                HealthStatus.Healthy => "✅ All systems healthy",
                HealthStatus.Warning => $"⚠️ {report.WarningCount} warnings detected",
                HealthStatus.Unhealthy => $"❌ {report.UnhealthyCount} issues detected",
                HealthStatus.Critical => $"🚨 {report.CriticalCount} critical issues detected",
                _ => "❓ Unknown status"
            };
        }
    }
}