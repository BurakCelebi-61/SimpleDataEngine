namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Segment metadata
    /// </summary>
    public partial class SegmentMetadata
    {
        public int SegmentId { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public long FileSizeBytes { get; set; }
        public string Checksum { get; set; }

        // Orijinal property'ler
        public long RecordCount { get; set; }
        public long SizeBytes { get; set; }
        public bool IsCompressed { get; set; }
        public double CompressionRatio { get; set; }
        public bool IsEncrypted { get; set; }
        public Dictionary<string, object> Statistics { get; set; } = new();
        public List<string> DeletedIds { get; set; } = new();
    }
}
