namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Individual notification
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public NotificationSeverity Severity { get; set; }
        public NotificationCategory Category { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public string Source { get; set; }
        public List<NotificationChannel> Channels { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? LastRetryAt { get; set; }
        public bool IsExpired => ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value;
        public TimeSpan Age => DateTime.Now - CreatedAt;

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.Now;
        }
    }
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}