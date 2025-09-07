namespace SimpleDataEngine.Notifications
{
    /// <summary>
    /// Migration result
    /// </summary>
    public class MigrationResult
    {
        public bool Success { get; set; }
        public string Version { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }