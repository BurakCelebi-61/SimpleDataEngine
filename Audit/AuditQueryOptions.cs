namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit query options
    /// </summary>
    public class AuditQueryOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<AuditLevel> Levels { get; set; }
        public List<AuditCategory> Categories { get; set; }
        public List<string> EventTypes { get; set; }
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string SearchText { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 100;
        public bool IncludeExceptions { get; set; } = true;
        public bool SortDescending { get; set; } = true;
    }
}