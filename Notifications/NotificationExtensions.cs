namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
    public static class NotificationExtensions
    {
        /// <summary>
        /// Creates a formatted summary of notification statistics
        /// </summary>
        /// <param name="stats">Notification statistics</param>
        /// <returns>Formatted summary</returns>
        public static string ToSummaryString(this NotificationStatistics stats)
        {
            var summary = new System.Text.StringBuilder();

            summary.AppendLine($"📢 Notification Statistics");
            summary.AppendLine($"  📈 Total: {stats.TotalNotifications:N0}");
            summary.AppendLine($"  📬 Unread: {stats.UnreadNotifications:N0}");
            summary.AppendLine($"  ⏰ Expired: {stats.ExpiredNotifications:N0}");
            summary.AppendLine($"  📊 Avg/Day: {stats.AverageNotificationsPerDay:F1}");
            summary.AppendLine($"  🔔 Subscriptions: {stats.ActiveSubscriptions}");

            if (stats.CountsBySeverity.Any())
            {
                summary.AppendLine();
                summary.AppendLine($"📊 By Severity:");
                foreach (var kvp in stats.CountsBySeverity.OrderByDescending(x => (int)x.Key))
                {
                    var icon = kvp.Key switch
                    {
                        NotificationSeverity.Critical => "🚨",
                        NotificationSeverity.Error => "❌",
                        NotificationSeverity.Warning => "⚠️",
                        NotificationSeverity.Info => "ℹ️",
                        _ => "•"
                    };
                    summary.AppendLine($"  {icon} {kvp.Key}: {kvp.Value:N0}");
                }
            }

            if (stats.CountsByCategory.Any())
            {
                summary.AppendLine();
                summary.AppendLine($"🏷️ By Category:");
                foreach (var kvp in stats.CountsByCategory.OrderByDescending(x => x.Value).Take(5))
                {
                    summary.AppendLine($"  • {kvp.Key}: {kvp.Value:N0}");
                }
            }

            return summary.ToString().TrimEnd();
        }

        /// <summary>
        /// Creates a quick notification subscription for critical issues
        /// </summary>
        /// <param name="channels">Channels to receive notifications on</param>
        /// <returns>Notification subscription</returns>
        public static NotificationSubscription CreateCriticalAlertSubscription(params NotificationChannel[] channels)
        {
            return new NotificationSubscription
            {
                Name = "Critical Alerts",
                Severities = new List<NotificationSeverity> { NotificationSeverity.Critical },
                Channels = channels.ToList(),
                Enabled = true
            };
        }

        /// <summary>
        /// Creates a health monitoring subscription
        /// </summary>
        /// <param name="channels">Channels to receive notifications on</param>
        /// <returns>Notification subscription</returns>
        public static NotificationSubscription CreateHealthMonitoringSubscription(params NotificationChannel[] channels)
        {
            return new NotificationSubscription
            {
                Name = "Health Monitoring",
                Categories = new List<NotificationCategory> { NotificationCategory.Health },
                Severities = new List<NotificationSeverity>
                {
                    NotificationSeverity.Warning,
                    NotificationSeverity.Error,
                    NotificationSeverity.Critical
                },
                Channels = channels.ToList(),
                Enabled = true
            };
        }

        /// <summary>
        /// Sends a simple info notification
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <returns>Delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> SendInfoAsync(string title, string message)
        {
            return await NotificationEngine.NotifyAsync(title, message, NotificationSeverity.Info);
        }

        /// <summary>
        /// Sends a warning notification
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <returns>Delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> SendWarningAsync(string title, string message)
        {
            return await NotificationEngine.NotifyAsync(title, message, NotificationSeverity.Warning);
        }

        /// <summary>
        /// Sends an error notification
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <returns>Delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> SendErrorAsync(string title, string message)
        {
            return await NotificationEngine.NotifyAsync(title, message, NotificationSeverity.Error);
        }

        /// <summary>
        /// Sends a critical notification
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <returns>Delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> SendCriticalAsync(string title, string message)
        {
            return await NotificationEngine.NotifyAsync(title, message, NotificationSeverity.Critical);
        }
    }
}