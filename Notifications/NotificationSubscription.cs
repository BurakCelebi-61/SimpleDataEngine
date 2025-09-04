namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Notification subscription
    /// </summary>
    public class NotificationSubscription
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public List<NotificationCategory> Categories { get; set; } = new();
        public List<NotificationSeverity> Severities { get; set; } = new();
        public List<NotificationChannel> Channels { get; set; } = new();
        public Func<Notification, bool> Filter { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();

        public bool ShouldReceive(Notification notification)
        {
            if (!Enabled)
                return false;

            if (Categories.Any() && !Categories.Contains(notification.Category))
                return false;

            if (Severities.Any() && !Severities.Contains(notification.Severity))
                return false;

            if (Filter != null && !Filter(notification))
                return false;

            return true;
        }
    }
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}