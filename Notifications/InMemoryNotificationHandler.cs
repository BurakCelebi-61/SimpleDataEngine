namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>
    internal class InMemoryNotificationHandler : INotificationHandler
    {
        public NotificationChannel Channel => NotificationChannel.InMemory;
        public string Name => "InMemory";

        public Task<NotificationDeliveryResult> SendAsync(Notification notification)
        {
            return Task.FromResult(new NotificationDeliveryResult
            {
                NotificationId = notification.Id,
                Channel = Channel,
                Success = true,
                DeliveryData = { ["StoredInMemory"] = true }
            });
        }

        public bool CanHandle(Notification notification) => true;
    }
    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}