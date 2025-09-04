namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Interface for notification handlers
    /// </summary>
    public interface INotificationHandler
    {
        NotificationChannel Channel { get; }
        string Name { get; }
        Task<NotificationDeliveryResult> SendAsync(Notification notification);
        bool CanHandle(Notification notification);
    }
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}