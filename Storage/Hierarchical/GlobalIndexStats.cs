namespace SimpleDataEngine.Storage.Hierarchical
{
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