namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// System resource statistics
    /// </summary>
    public class ResourceStatistics
    {
        public long CurrentMemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        public long AverageMemoryUsage { get; set; }
        public int GarbageCollections { get; set; }
        public int ActiveThreads { get; set; }
        public TimeSpan SystemUpTime { get; set; }
    }
}