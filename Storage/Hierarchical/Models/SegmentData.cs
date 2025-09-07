namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Segment data structure
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public partial class SegmentData<T>
    {
        /// <summary>
        /// EKSIK PROPERTY - Segment ID
        /// </summary>
        public int SegmentId { get; set; }

        /// <summary>
        /// EKSIK PROPERTY - Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// EKSIK PROPERTY - Last modification timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// EKSIK PROPERTY - Records list (main data)
        /// </summary>
        public List<T> Records { get; set; } = new List<T>();

        /// <summary>
        /// Segment header information
        /// </summary>
        public SegmentHeader Header { get; set; } = new();

        /// <summary>
        /// Entity data list (alias for Records - backward compatibility)
        /// </summary>
        public List<T> Data
        {
            get => Records;
            set => Records = value ?? new List<T>();
        }

        /// <summary>
        /// Segment metadata
        /// </summary>
        public SegmentMetadata Metadata { get; set; } = new();

        /// <summary>
        /// Record count (computed property)
        /// </summary>
        public int RecordCount => Records?.Count ?? 0;

        /// <summary>
        /// Is segment empty
        /// </summary>
        public bool IsEmpty => RecordCount == 0;

        /// <summary>
        /// Segment version for compatibility tracking
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Segment compression status
        /// </summary>
        public bool IsCompressed { get; set; } = false;

        /// <summary>
        /// Checksum for data integrity
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SegmentData()
        {
            Records = new List<T>();
            CreatedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with segment ID
        /// </summary>
        public SegmentData(int segmentId) : this()
        {
            SegmentId = segmentId;
        }

        /// <summary>
        /// Constructor with records
        /// </summary>
        public SegmentData(List<T> records) : this()
        {
            Records = records ?? new List<T>();
        }

        /// <summary>
        /// Constructor with segment ID and records
        /// </summary>
        public SegmentData(int segmentId, List<T> records) : this(records)
        {
            SegmentId = segmentId;
        }

        /// <summary>
        /// Add record to segment
        /// </summary>
        public void AddRecord(T record)
        {
            if (record != null)
            {
                Records.Add(record);
                LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Add multiple records to segment
        /// </summary>
        public void AddRecords(IEnumerable<T> records)
        {
            if (records != null && records.Any())
            {
                Records.AddRange(records);
                LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Remove record from segment
        /// </summary>
        public bool RemoveRecord(T record)
        {
            if (Records.Remove(record))
            {
                LastModified = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all records
        /// </summary>
        public void Clear()
        {
            Records.Clear();
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Update last modified timestamp
        /// </summary>
        public void Touch()
        {
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Segment header
        /// </summary>
        public class SegmentHeader
        {
            public string FileName { get; set; }
            public int SequenceNumber { get; set; }
            public string PreviousSegment { get; set; }
            public string NextSegment { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime LastModified { get; set; } = DateTime.UtcNow;
            public string Checksum { get; set; }
            public EntityMetadata.IdRange IdRange { get; set; } = new();
            public string EntityType { get; set; }
            public string Version { get; set; } = "1.0.0";
        }
    }
}