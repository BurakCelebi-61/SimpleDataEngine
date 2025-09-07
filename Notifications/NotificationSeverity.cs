namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Notification severity levels
    /// </summary>
    public enum NotificationSeverity
    {
        /// <summary>
        /// Informational message
        /// </summary>
        Information = 0,

        /// <summary>
        /// Debug information (lowest priority)
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Success notification
        /// </summary>
        Success = 2,

        /// <summary>
        /// Warning message
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Error message
        /// </summary>
        Error = 4,

        /// <summary>
        /// Critical system error
        /// </summary>
        Critical = 5,

        /// <summary>
        /// Fatal system error
        /// </summary>
        Fatal = 6
    }
}