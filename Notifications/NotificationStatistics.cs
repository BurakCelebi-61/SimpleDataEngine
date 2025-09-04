namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Notification statistics
    /// </summary>
    public class NotificationStatistics
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int ExpiredNotifications { get; set; }
        public DateTime OldestNotification { get; set; }
        public DateTime NewestNotification { get; set; }
        public Dictionary<NotificationSeverity, int> CountsBySeverity { get; set; } = new();
        public Dictionary<NotificationCategory, int> CountsByCategory { get; set; } = new();
        public Dictionary<NotificationChannel, int> CountsByChannel { get; set; } = new();
        public double AverageNotificationsPerDay { get; set; }
        public int ActiveSubscriptions { get; set; }
    }
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}