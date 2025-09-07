using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Segment metadata
    /// </summary>
    public class SegmentMetadata
    {
        public long RecordCount { get; set; }
        public long SizeBytes { get; set; }
        public bool IsCompressed { get; set; }
        public double CompressionRatio { get; set; }
        public bool IsEncrypted { get; set; }
        public Dictionary<string, object> Statistics { get; set; } = new();
        public List<string> DeletedIds { get; set; } = new(); // Soft delete tracking
    }
}
