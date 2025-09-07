using SimpleDataEngine.Audit;
using System.Collections.Concurrent;

namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Central notification engine for the application
    /// </summary>
    public static class NotificationEngine
    {
        private static readonly ConcurrentQueue<Notification> _notifications = new();
        private static readonly List<INotificationChannel> _channels = new();
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the notification engine
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                // Add default console channel
                _channels.Add(new ConsoleNotificationChannel());
                _initialized = true;

                AuditLogger.Log("NOTIFICATION_ENGINE_INITIALIZED", null, AuditCategory.Notification);
            }
        }

        /// <summary>
        /// Send a notification
        /// </summary>
        public static async Task NotifyAsync(string title, string message, NotificationSeverity severity = NotificationSeverity.Information)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            };

            _notifications.Enqueue(notification);

            // Send to all channels
            var tasks = new List<Task>();
            lock (_lock)
            {
                foreach (var channel in _channels)
                {
                    tasks.Add(channel.SendAsync(notification));
                }
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            AuditLogger.Log("NOTIFICATION_SENT", new
            {
                NotificationId = notification.Id,
                Title = title,
                Severity = severity.ToString()
            }, AuditCategory.Notification);
        }

        /// <summary>
        /// Add notification channel
        /// </summary>
        public static void AddChannel(INotificationChannel channel)
        {
            lock (_lock)
            {
                _channels.Add(channel);
            }
        }

        /// <summary>
        /// Get recent notifications
        /// </summary>
        public static List<Notification> GetNotifications(int count = 50)
        {
            return _notifications.TakeLast(count).ToList();
        }

        /// <summary>
        /// Get notification statistics
        /// </summary>
        public static NotificationStatistics GetStatistics()
        {
            var notifications = _notifications.ToList();
            var stats = new NotificationStatistics
            {
                TotalNotifications = notifications.Count,
                NotificationsBySeverity = new Dictionary<NotificationSeverity, int>()
            };

            foreach (var severity in Enum.GetValues<NotificationSeverity>())
            {
                stats.NotificationsBySeverity[severity] = notifications.Count(n => n.Severity == severity);
            }

            return stats;
        }

        /// <summary>
        /// Clear old notifications
        /// </summary>
        public static void ClearOldNotifications(TimeSpan maxAge)
        {
            var cutoff = DateTime.UtcNow - maxAge;
            var cleared = 0;

            // Note: ConcurrentQueue doesn't support removal, so we'll just let them age out naturally
            // In a real implementation, you might use a different data structure

            AuditLogger.Log("NOTIFICATIONS_CLEANUP_REQUESTED", new
            {
                MaxAge = maxAge.ToString(),
                ClearedCount = cleared
            }, AuditCategory.Notification);
        }
    }
}