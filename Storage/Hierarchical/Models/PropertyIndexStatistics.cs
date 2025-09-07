namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Property index statistics
    /// </summary>
    public class PropertyIndexStatistics
    {
        public int TotalEntries { get; set; }
        public int UniqueValues { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public double AverageEntriesPerValue { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
    }
}