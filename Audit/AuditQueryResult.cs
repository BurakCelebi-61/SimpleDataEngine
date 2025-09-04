namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit query result
    /// </summary>
    public class AuditQueryResult
    {
        public List<AuditEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public bool HasMore => Skip + Take < TotalCount;
        public TimeSpan QueryDuration { get; set; }
    }
}