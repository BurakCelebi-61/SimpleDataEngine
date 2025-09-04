namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit statistics
    /// </summary>
    public class AuditStatistics
    {
        public int TotalEntries { get; set; }
        public DateTime OldestEntry { get; set; }
        public DateTime NewestEntry { get; set; }
        public Dictionary<AuditLevel, int> CountsByLevel { get; set; } = new();
        public Dictionary<AuditCategory, int> CountsByCategory { get; set; } = new();
        public Dictionary<string, int> CountsByEventType { get; set; } = new();
        public long TotalSizeBytes { get; set; }
        public TimeSpan TimeSpan => NewestEntry - OldestEntry;
        public double AverageEntriesPerDay => TimeSpan.TotalDays > 0 ? TotalEntries / TimeSpan.TotalDays : 0;
    }
}