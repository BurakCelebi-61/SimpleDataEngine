namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public class LoggingConfig
    {
        public string LogDirectory { get; set; } = "logs";
        public string LogLevel { get; set; } = "Info";
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 30;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
    }
}