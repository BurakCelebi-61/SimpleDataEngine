namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit logger configuration
    /// </summary>
    public class AuditLoggerOptions
    {
        public bool Enabled { get; set; } = true;
        public AuditLevel MinimumLevel { get; set; } = AuditLevel.Info;
        public string LogDirectory { get; set; }
        public string FileNameFormat { get; set; } = "audit_{0:yyyyMMdd}.json";
        public int MaxLogFileSizeMB { get; set; } = 50;
        public int MaxLogFiles { get; set; } = 30;
        public bool IncludeStackTrace { get; set; } = false;
        public bool IncludeSystemInfo { get; set; } = true;
        public bool AsyncLogging { get; set; } = true;
        public int BufferSize { get; set; } = 100;
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);
        public bool CompressOldLogs { get; set; } = true;
        public bool AutoCleanup { get; set; } = true;
    }
}