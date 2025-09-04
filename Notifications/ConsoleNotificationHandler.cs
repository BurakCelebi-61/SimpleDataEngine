namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Console notification handler
    /// </summary>
    internal class ConsoleNotificationHandler : INotificationHandler
    {
        public NotificationChannel Channel => NotificationChannel.Console;
        public string Name => "Console";

        public Task<NotificationDeliveryResult> SendAsync(Notification notification)
        {
            try
            {
                var icon = notification.Severity switch
                {
                    NotificationSeverity.Critical => "🚨",
                    NotificationSeverity.Error => "❌",
                    NotificationSeverity.Warning => "⚠️",
                    NotificationSeverity.Info => "ℹ️",
                    _ => "•"
                };

                var message = $"{icon} [{notification.Severity}] {notification.Title}";
                if (!string.IsNullOrWhiteSpace(notification.Message))
                    message += $": {notification.Message}";

                Console.WriteLine(message);

                return Task.FromResult(new NotificationDeliveryResult
                {
                    NotificationId = notification.Id,
                    Channel = Channel,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new NotificationDeliveryResult
                {
                    NotificationId = notification.Id,
                    Channel = Channel,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public bool CanHandle(Notification notification) => true;
    }
    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}