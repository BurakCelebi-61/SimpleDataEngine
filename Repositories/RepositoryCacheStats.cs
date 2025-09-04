namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository cache statistics
    /// </summary>
    public class RepositoryCacheStats
    {
        public int TotalRequests { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;
        public DateTime LastReset { get; set; } = DateTime.Now;
        public int CachedItemCount { get; set; }
        public long MemoryUsageBytes { get; set; }
    }
}