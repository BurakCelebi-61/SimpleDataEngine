namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Global cross-entity index
    /// </summary>
    public class GlobalIndex
    {
        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Entity name to directory path mapping
        /// </summary>
        public Dictionary<string, string> EntityLocations { get; set; } = new();

        /// <summary>
        /// Cross-entity relationship mapping
        /// </summary>
        public Dictionary<string, string> RelationshipMap { get; set; } = new();

        /// <summary>
        /// Global database statistics
        /// </summary>
        public GlobalIndexStats Stats { get; set; } = new();

        /// <summary>
        /// Entity dependency graph
        /// </summary>
        public Dictionary<string, List<string>> EntityDependencies { get; set; } = new();

        /// <summary>
        /// Global statistics
        /// </summary>
        public class GlobalStats
        {
            public int TotalSegments { get; set; }
            public double AverageSegmentSizeMB { get; set; }
            public double FragmentationLevel { get; set; }
            public DateTime LastOptimized { get; set; }
            public long TotalIndexSize { get; set; }
            public double IndexEfficiency { get; set; }
        }
    }
    public class GlobalIndexStats
    {
        public int TotalSegments { get; set; }
        public double AverageSegmentSizeMB { get; set; }
        public double FragmentationLevel { get; set; }
        public DateTime LastOptimized { get; set; }
        public long TotalIndexSize { get; set; }
        public double IndexEfficiency { get; set; }
    }
}