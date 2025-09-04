namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Performance configuration settings
    /// </summary>
    public class PerformanceConfig
    {
        public bool EnablePerformanceTracking { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 30;
        public int MaxCacheItems { get; set; } = 1000;
        public bool EnableQueryOptimization { get; set; } = true;
        public int SlowQueryThresholdMs { get; set; } = 1000;
    }
}