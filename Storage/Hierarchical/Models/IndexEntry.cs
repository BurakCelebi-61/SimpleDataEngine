namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Index entry for property-based indexing
    /// </summary>
    public class IndexEntry
    {
        /// <summary>
        /// Record ID that this index entry points to
        /// </summary>
        public object RecordId { get; set; }

        /// <summary>
        /// Property value being indexed
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Segment ID where the record is stored
        /// </summary>
        public int SegmentId { get; set; }

        /// <summary>
        /// Position within the segment
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// When this index entry was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this index entry was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Property name being indexed
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Data type of the indexed property
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// Hash code for quick comparison
        /// </summary>
        public string HashCode { get; set; }

        public IndexEntry()
        {
        }

        public IndexEntry(object recordId, object value, int segmentId, int position = 0)
        {
            RecordId = recordId;
            Value = value;
            SegmentId = segmentId;
            Position = position;
            CreatedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        public override bool Equals(object obj)
        {
            if (obj is IndexEntry other)
            {
                return Equals(RecordId, other.RecordId) &&
                       SegmentId == other.SegmentId &&
                       Equals(Value, other.Value);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RecordId, SegmentId, Value);
        }

        public override string ToString()
        {
            return $"IndexEntry[RecordId={RecordId}, Value={Value}, Segment={SegmentId}]";
        }
    }
}