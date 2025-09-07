namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Entity-specific index
    /// </summary>
    public class EntityIndex
    {
        /// <summary>
        /// Entity type name
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// ID range to segment file mapping
        /// </summary>
        public Dictionary<string, string> IdToSegment { get; set; } = new();

        /// <summary>
        /// Property-based indexes for query optimization
        /// </summary>
        public Dictionary<string, Dictionary<string, PropertyIndexInfo>> PropertyIndexes { get; set; } = new();

        /// <summary>
        /// Index statistics
        /// </summary>
        public IndexStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Cached query results
        /// </summary>
        public Dictionary<string, CachedQuery> QueryCache { get; set; } = new();

        /// <summary>
        /// Property index information
        /// </summary>
        public class PropertyIndexInfo
        {
            public object[] Range { get; set; } = new object[2]; // [min, max]
            public long Count { get; set; }
            public List<object> SampleValues { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.Now;
            public IndexType IndexType { get; set; } = IndexType.Range;
        }

        /// <summary>
        /// Index statistics
        /// </summary>
        public class IndexStatistics
        {
            public long AvgRecordsPerSegment { get; set; }
            public string IndexUpdateFrequency { get; set; } = "realtime";
            public DateTime LastOptimized { get; set; }
            public double IndexHitRatio { get; set; }
            public long TotalQueries { get; set; }
            public long IndexedQueries { get; set; }
        }

        /// <summary>
        /// Cached query information
        /// </summary>
        public class CachedQuery
        {
            public string QueryHash { get; set; }
            public List<string> ResultSegments { get; set; } = new();
            public DateTime CachedAt { get; set; } = DateTime.Now;
            public DateTime ExpiresAt { get; set; }
            public long ResultCount { get; set; }
        }

        /// <summary>
        /// Index types
        /// </summary>
        public enum IndexType
        {
            Range,
            Hash,
            Unique,
            Composite
        }
    }
}