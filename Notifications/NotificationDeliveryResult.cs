namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Notification delivery result
    /// </summary>
    public class NotificationDeliveryResult
    {
        public Guid NotificationId { get; set; }
        public NotificationChannel Channel { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime DeliveredAt { get; set; } = DateTime.Now;
        public TimeSpan DeliveryDuration { get; set; }
        public Dictionary<string, object> DeliveryData { get; set; } = new();
    }
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}