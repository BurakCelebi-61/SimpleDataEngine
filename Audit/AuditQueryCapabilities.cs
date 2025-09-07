namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Describes the query capabilities of an audit target
    /// </summary>
    public class AuditQueryCapabilities
    {
        /// <summary>
        /// Whether text search is supported
        /// </summary>
        public bool SupportsTextSearch { get; set; }

        /// <summary>
        /// Whether date range filtering is supported
        /// </summary>
        public bool SupportsDateRangeFiltering { get; set; }

        /// <summary>
        /// Whether level filtering is supported
        /// </summary>
        public bool SupportsLevelFiltering { get; set; }

        /// <summary>
        /// Whether category filtering is supported
        /// </summary>
        public bool SupportsCategoryFiltering { get; set; }

        /// <summary>
        /// Whether sorting is supported
        /// </summary>
        public bool SupportsSorting { get; set; }

        /// <summary>
        /// Whether pagination is supported
        /// </summary>
        public bool SupportsPagination { get; set; }

        /// <summary>
        /// Whether grouping operations are supported
        /// </summary>
        public bool SupportsGrouping { get; set; }

        /// <summary>
        /// Whether aggregation functions are supported
        /// </summary>
        public bool SupportsAggregation { get; set; }

        /// <summary>
        /// Maximum number of results per query
        /// </summary>
        public int MaxResults { get; set; } = 1000;

        /// <summary>
        /// Supported sort fields
        /// </summary>
        public List<string> SupportedSortFields { get; set; } = new List<string>();

        /// <summary>
        /// Supported filter fields
        /// </summary>
        public List<string> SupportedFilterFields { get; set; } = new List<string>();
    }
}