namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Console notification channel
    /// </summary>
    public class ConsoleNotificationChannel : INotificationChannel
    {
        public async Task SendAsync(Notification notification)
        {
            await Task.Run(() =>
            {
                var color = notification.Severity switch
                {
                    NotificationSeverity.Information => ConsoleColor.White,
                    NotificationSeverity.Warning => ConsoleColor.Yellow,
                    NotificationSeverity.Error => ConsoleColor.Red,
                    NotificationSeverity.Critical => ConsoleColor.Magenta,
                    _ => ConsoleColor.White
                };

                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                Console.WriteLine($"[{notification.Severity.ToString().ToUpper()}] {notification.Title}: {notification.Message}");

                Console.ForegroundColor = originalColor;
            });
        }
    }
