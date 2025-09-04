using SimpleDataEngine.Configuration;

namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// File notification handler
    /// </summary>
    internal class FileNotificationHandler : INotificationHandler
    {
        public NotificationChannel Channel => NotificationChannel.File;
        public string Name => "File";
        private readonly string _logPath;

        public FileNotificationHandler()
        {
            var dataDirectory = ConfigManager.Current?.Database?.DataDirectory ??
                               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleDataEngine");

            _logPath = Path.Combine(dataDirectory, "Notifications", "notifications.log");

            var directory = Path.GetDirectoryName(_logPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task<NotificationDeliveryResult> SendAsync(Notification notification)
        {
            try
            {
                var logEntry = $"[{notification.CreatedAt:yyyy-MM-dd HH:mm:ss}] [{notification.Severity}] [{notification.Category}] {notification.Title}";
                if (!string.IsNullOrWhiteSpace(notification.Message))
                    logEntry += $": {notification.Message}";

                logEntry += Environment.NewLine;

                await File.AppendAllTextAsync(_logPath, logEntry);

                return new NotificationDeliveryResult
                {
                    NotificationId = notification.Id,
                    Channel = Channel,
                    Success = true,
                    DeliveryData = { ["LogPath"] = _logPath }
                };
            }
            catch (Exception ex)
            {
                return new NotificationDeliveryResult
                {
                    NotificationId = notification.Id,
                    Channel = Channel,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public bool CanHandle(Notification notification) => true;
    }
    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}