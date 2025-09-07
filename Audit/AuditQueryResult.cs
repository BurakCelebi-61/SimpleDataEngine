namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Result of audit query operations
    /// </summary>
    public class AuditQueryResult
    {
        /// <summary>
        /// Whether the query was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if query failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Query result entries
        /// </summary>
        public List<AuditEntry> Entries { get; set; } = new List<AuditEntry>();

        /// <summary>
        /// Total count of matching entries
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Query execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Additional metadata about the query
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful query result
        /// </summary>
        public static AuditQueryResult Success(List<AuditEntry> entries, int totalCount = 0)
        {
            return new AuditQueryResult
            {
                Success = true,
                Entries = entries,
                TotalCount = totalCount > 0 ? totalCount : entries.Count
            };
        }

        /// <summary>
        /// Creates a failed query result
        /// </summary>
        public static AuditQueryResult Failure(string errorMessage)
        {
            return new AuditQueryResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Entries = new List<AuditEntry>()
            };
        }
    }
}