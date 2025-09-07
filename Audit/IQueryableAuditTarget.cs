namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Interface for audit targets that support querying operations
    /// </summary>
    public interface IQueryableAuditTarget : IAuditTarget
    {
        /// <summary>
        /// Queries audit entries based on specified criteria
        /// </summary>
        /// <param name="options">Query options and filters</param>
        /// <returns>Query result with matching entries</returns>
        Task<AuditQueryResult> QueryAsync(AuditQueryOptions options);

        /// <summary>
        /// Gets statistics about audit entries
        /// </summary>
        /// <param name="startDate">Start date for statistics</param>
        /// <param name="endDate">End date for statistics</param>
        /// <returns>Audit statistics for the specified period</returns>
        Task<AuditStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Counts total entries matching the specified criteria
        /// </summary>
        /// <param name="options">Query options for counting</param>
        /// <returns>Number of matching entries</returns>
        Task<int> CountAsync(AuditQueryOptions options);

        /// <summary>
        /// Checks if the target supports advanced querying features
        /// </summary>
        bool SupportsAdvancedQuery { get; }

        /// <summary>
        /// Gets the maximum number of entries that can be returned in a single query
        /// </summary>
        int MaxQueryResults { get; }

        /// <summary>
        /// Searches audit entries by text content
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="options">Additional search options</param>
        /// <returns>Matching audit entries</returns>
        Task<AuditQueryResult> SearchAsync(string searchText, AuditQueryOptions? options = null);

        /// <summary>
        /// Gets distinct values for a specific field
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="options">Query options for filtering</param>
        /// <returns>List of distinct values</returns>
        Task<List<object>> GetDistinctValuesAsync(string fieldName, AuditQueryOptions? options = null);

        /// <summary>
        /// Gets audit entries grouped by a specific field
        /// </summary>
        /// <param name="groupByField">Field to group by</param>
        /// <param name="options">Query options</param>
        /// <returns>Grouped audit results</returns>
        Task<Dictionary<object, List<AuditEntry>>> GroupByAsync(string groupByField, AuditQueryOptions? options = null);

        /// <summary>
        /// Validates query options before execution
        /// </summary>
        /// <param name="options">Query options to validate</param>
        /// <returns>True if options are valid</returns>
        bool ValidateQueryOptions(AuditQueryOptions options);

        /// <summary>
        /// Gets the query capabilities of this target
        /// </summary>
        /// <returns>Query capabilities information</returns>
        AuditQueryCapabilities GetQueryCapabilities();
    }
}