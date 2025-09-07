namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Global database metadata
    /// </summary>
    public class GlobalMetadata
    {
        /// <summary>
        /// Database version
        /// </summary>
        public string DatabaseVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Database creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether encryption is enabled for this database
        /// </summary>
        public bool EncryptionEnabled { get; set; }

        /// <summary>
        /// Total number of entity types
        /// </summary>
        public int TotalEntities { get; set; }

        /// <summary>
        /// Total number of records across all entities
        /// </summary>
        public long TotalRecords { get; set; }

        /// <summary>
        /// Total database size in MB
        /// </summary>
        public long TotalSizeMB { get; set; }

        /// <summary>
        /// List of entities in database
        /// </summary>
        public List<EntityInfo> Entities { get; set; } = new();

        /// <summary>
        /// Database configuration snapshot
        /// </summary>
        public DatabaseConfigSnapshot ConfigSnapshot { get; set; } = new();

        /// <summary>
        /// Entity information
        /// </summary>
        public class EntityInfo
        {
            public string Name { get; set; }
            public long RecordCount { get; set; }
            public int SegmentCount { get; set; }
            public long SizeMB { get; set; }
            public DateTime LastUpdated { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsActive { get; set; } = true;
        }

        /// <summary>
        /// Database configuration snapshot
        /// </summary>
        public class DatabaseConfigSnapshot
        {
            public int MaxSegmentSizeMB { get; set; }
            public int MaxRecordsPerSegment { get; set; }
            public bool EnableCompression { get; set; }
            public bool EnableRealTimeIndexing { get; set; }
            public int AutoCleanupDays { get; set; }
        }
    }
}