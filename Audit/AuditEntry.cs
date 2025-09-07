namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit entry for query results - compatible with AuditLogEntry
    /// </summary>
    public class AuditEntry
    {
        /// <summary>
        /// Unique identifier for the audit entry
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the audited event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Severity level of the audit entry
        /// </summary>
        public AuditLevel Level { get; set; }

        /// <summary>
        /// Category of the audited operation
        /// </summary>
        public AuditCategory Category { get; set; }

        /// <summary>
        /// Audit message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Associated data (serialized as JSON)
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// Thread ID where the operation occurred
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Machine name where the operation occurred
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// User identifier
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Session identifier
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Exception information (serialized)
        /// </summary>
        public string? ExceptionData { get; set; }

        /// <summary>
        /// Source file name (for debugging)
        /// </summary>
        public string? SourceFile { get; set; }

        /// <summary>
        /// Source line number (for debugging)
        /// </summary>
        public int? SourceLine { get; set; }

        /// <summary>
        /// Creates AuditEntry from AuditLogEntry
        /// </summary>
        public static AuditEntry FromLogEntry(AuditLogEntry logEntry)
        {
            return new AuditEntry
            {
                Timestamp = logEntry.Timestamp,
                Level = logEntry.Level,
                Category = logEntry.Category,
                Message = logEntry.Message,
                Data = logEntry.Data != null ? System.Text.Json.JsonSerializer.Serialize(logEntry.Data) : null,
                ThreadId = logEntry.ThreadId,
                MachineName = logEntry.MachineName,
                UserId = logEntry.UserId,
                SessionId = logEntry.SessionId,
                ExceptionData = logEntry.Exception != null ? System.Text.Json.JsonSerializer.Serialize(new
                {
                    Message = logEntry.Exception.Message,
                    StackTrace = logEntry.Exception.StackTrace,
                    Type = logEntry.Exception.GetType().Name
                }) : null
            };
        }

        /// <summary>
        /// Converts to AuditLogEntry
        /// </summary>
        public AuditLogEntry ToLogEntry()
        {
            var logEntry = new AuditLogEntry
            {
                Timestamp = Timestamp,
                Level = Level,
                Category = Category,
                Message = Message,
                ThreadId = ThreadId,
                MachineName = MachineName,
                UserId = UserId,
                SessionId = SessionId
            };

            // Deserialize data if present
            if (!string.IsNullOrEmpty(Data))
            {
                try
                {
                    logEntry.Data = System.Text.Json.JsonSerializer.Deserialize<object>(Data);
                }
                catch
                {
                    logEntry.Data = Data; // Fallback to string
                }
            }

            // Deserialize exception if present
            if (!string.IsNullOrEmpty(ExceptionData))
            {
                try
                {
                    var exData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ExceptionData);
                    if (exData != null && exData.ContainsKey("Message"))
                    {
                        logEntry.Exception = new Exception(exData["Message"].ToString());
                    }
                }
                catch
                {
                    // Ignore deserialization errors
                }
            }

            return logEntry;
        }
    }
}