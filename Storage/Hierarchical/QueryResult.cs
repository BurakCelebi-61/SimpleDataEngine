namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Query result information
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class QueryResult<T>
    {
        /// <summary>
        /// Query results
        /// </summary>
        public List<T> Results { get; set; } = new();

        /// <summary>
        /// Total count (may be different from Results.Count if paged)
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Segments accessed during query
        /// </summary>
        public List<string> AccessedSegments { get; set; } = new();

        /// <summary>
        /// Query execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Whether result came from cache
        /// </summary>
        public bool FromCache { get; set; }

        /// <summary>
        /// Query metadata
        /// </summary>
        public QueryMetadata Metadata { get; set; } = new();

        /// <summary>
        /// Query metadata
        /// </summary>
        public class QueryMetadata
        {
            public string QueryHash { get; set; }
            public DateTime ExecutedAt { get; set; } = DateTime.Now;
            public bool UsedIndex { get; set; }
            public List<string> IndexesUsed { get; set; } = new();
            public long RecordsScanned { get; set; }
            public double IndexHitRatio { get; set; }
        }
    }
}