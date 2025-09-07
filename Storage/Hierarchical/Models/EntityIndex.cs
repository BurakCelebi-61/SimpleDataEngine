using static SimpleDataEngine.Storage.Hierarchical.Models.EntityMetadata;

namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Entity-specific index
    /// </summary>
    public partial class EntityIndex
    {
        /// <summary>
        /// Entity type name
        /// </summary>
        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public long? TotalRecords { get; set; }

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
        public List<SegmentMetadata> Segments { get; set; } = new();
        public EntitySchema Schema { get; set; } = new();
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

        public class EntitySchema
        {
            public string Version { get; set; } = "1.0.0";
            public List<PropertyInfo> Properties { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.Now;

            public class PropertyInfo
            {
                public string Name { get; set; }
                public string Type { get; set; }
                public bool IsIndexed { get; set; }
                public bool IsRequired { get; set; }
            }
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