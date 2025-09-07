using SimpleDataEngine.Storage.Hierarchical.Managers;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Storage statistics information
    /// </summary>
    public class StorageStatistics
    {
        public string EntityName { get; set; }
        public long TotalRecords { get; set; }
        public int TotalSegments { get; set; }
        public long TotalSizeBytes { get; set; }
        public IndexStatistics IndexStatistics { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
    }
}