namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Configuration options for audit logger
    /// </summary>
    public class AuditLoggerOptions
    {
        public string? LogDirectory { get; set; } = "logs";
        public string FileNameFormat { get; set; } = "audit-{0:yyyy-MM-dd}.log";
        public int MaxFileSizeMB { get; set; } = 100;
        public int MaxFiles { get; set; } = 30;
        public bool CompressOldLogs { get; set; } = true;
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxBufferedEntries { get; set; } = 1000;
        public bool EnableAsyncFlush { get; set; } = true;
    }
}