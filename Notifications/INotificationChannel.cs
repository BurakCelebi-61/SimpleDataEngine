namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Notification channel interface
    /// </summary>
    public interface INotificationChannel
    {
        Task SendAsync(Notification notification);
    }
}