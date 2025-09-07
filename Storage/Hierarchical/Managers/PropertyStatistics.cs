namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Property-specific statistics
    /// </summary>
    public class PropertyStatistics
    {
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public int UniqueValues { get; set; }
        public int TotalEntries { get; set; }
        public DateTime LastModified { get; set; }
    }
}