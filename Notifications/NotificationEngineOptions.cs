namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Notification engine options
    /// </summary>
    public class NotificationEngineOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxInMemoryNotifications { get; set; } = 1000;
        public TimeSpan DefaultNotificationExpiry { get; set; } = TimeSpan.FromDays(30);
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
        public bool AutoCleanupExpired { get; set; } = true;
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
        public NotificationSeverity MinimumSeverity { get; set; } = NotificationSeverity.Info;
        public List<NotificationChannel> DefaultChannels { get; set; } = new() { NotificationChannel.InMemory };
        public bool LogDeliveryFailures { get; set; } = true;
    }
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}