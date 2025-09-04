namespace SimpleDataEngine.Audit
{

    /// <summary>
    /// Individual audit log entry
    /// </summary>
    public class AuditEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public AuditLevel Level { get; set; }
        public AuditCategory Category { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string MachineName { get; set; } = Environment.MachineName;
        public string ApplicationName { get; set; } = "SimpleDataEngine";
        public Exception Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public TimeSpan? Duration { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }

        public bool IsError => Level == AuditLevel.Error || Level == AuditLevel.Critical;
        public bool IsWarning => Level == AuditLevel.Warning;
        public string FormattedMessage => FormatMessage();

        private string FormatMessage()
        {
            var formatted = $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Category}] {EventType}";

            if (!string.IsNullOrWhiteSpace(Message))
                formatted += $": {Message}";

            if (Duration.HasValue)
                formatted += $" (Duration: {Duration.Value.TotalMilliseconds:F0}ms)";

            return formatted;
        }
    }
}