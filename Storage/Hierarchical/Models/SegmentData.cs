namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Segment data structure
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class SegmentData<T>
    {
        /// <summary>
        /// Segment header information
        /// </summary>
        public SegmentHeader Header { get; set; } = new();

        /// <summary>
        /// Entity data list
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// Segment metadata
        /// </summary>
        public SegmentMetadata Metadata { get; set; } = new();

        /// <summary>
        /// Segment header
        /// </summary>
        public class SegmentHeader
        {
            public string FileName { get; set; }
            public int SequenceNumber { get; set; }
            public string PreviousSegment { get; set; }
            public string NextSegment { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public DateTime LastModified { get; set; } = DateTime.Now;
            public string Checksum { get; set; }
            public EntityMetadata.IdRange IdRange { get; set; } = new();
            public string EntityType { get; set; }
            public string Version { get; set; } = "1.0.0";
        }


    }
}