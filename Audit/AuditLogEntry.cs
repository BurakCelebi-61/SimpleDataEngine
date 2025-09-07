namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit log entry data structure
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public AuditLevel Level { get; set; }
        public AuditCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public int ThreadId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public Exception? Exception { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
}