namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Entity-specific metadata
    /// </summary>
    public class EntityMetadata
    {
        /// <summary>
        /// Entity type name
        /// </summary>
        public string EntityType { get; set; }
        public string EntityName { get; set; }  // Extension'dan gelen

        /// <summary>
        /// Current active segment number
        /// </summary>
        public int CurrentSegment { get; set; }

        /// <summary>
        /// Total records in this entity
        /// </summary>
        public long TotalRecords { get; set; }

        /// <summary>
        /// Total size in MB
        /// </summary>
        public long TotalSizeMB { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Entity creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// List of segments
        /// </summary>
        public List<SegmentInfo> Segments { get; set; } = new();

        /// <summary>
        /// Entity schema information
        /// </summary>
        public EntitySchema Schema { get; set; } = new();

        /// <summary>
        /// Segment information
        /// </summary>
        public class SegmentInfo
        {
            public string FileName { get; set; }
            public long RecordCount { get; set; }
            public long SizeMB { get; set; }
            public IdRange IdRange { get; set; } = new();
            public string Checksum { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastModified { get; set; }
            public bool IsActive { get; set; } = true;
            public bool IsCompressed { get; set; }
            public double CompressionRatio { get; set; }
        }

        /// <summary>
        /// ID range information
        /// </summary>
        public class IdRange
        {
            public int Min { get; set; }
            public int Max { get; set; }
            public int Count => Max - Min + 1;
        }

        /// <summary>
        /// Entity schema information
        /// </summary>
        public class EntitySchema
        {
            public string Version { get; set; } = "1.0.0";
            public List<PropertyInfo> Properties { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.Now;

            public class PropertyInfo
            {
                public string Name { get; set; }
                public string Type { get; set; }
                public bool IsIndexed { get; set; }
                public bool IsRequired { get; set; }
            }
        }
    }
}