namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Database statistics information
    /// </summary>
    public class DatabaseStatistics
    {
        public Guid DatabaseId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public int TotalEntities { get; set; }
        public bool EncryptionEnabled { get; set; }
        public string BasePath { get; set; }
        public List<EntityStatistics> EntityStatistics { get; set; } = new();
    }
}