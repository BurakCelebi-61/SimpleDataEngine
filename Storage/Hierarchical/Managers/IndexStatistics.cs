namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Index statistics information
    /// </summary>
    public class IndexStatistics
    {
        public string EntityName { get; set; }
        public long TotalRecords { get; set; }
        public int IndexedProperties { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public List<PropertyStatistics> PropertyStatistics { get; set; } = new();
    }
}