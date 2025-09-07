namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Individual entity statistics
    /// </summary>
    public class EntityStatistics
    {
        public string EntityName { get; set; }
        public long TotalRecords { get; set; }
        public int TotalSegments { get; set; }
        public long TotalSizeBytes { get; set; }
        public DateTime LastModified { get; set; }
    }
}