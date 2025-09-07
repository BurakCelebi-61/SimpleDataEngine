using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Type conversion extension methods
    /// </summary>
    public static class TypeConversionExtensions
    {
        /// <summary>
        /// List<SegmentMetadata> to List<EntityMetadata.SegmentInfo> conversion
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> ToSegmentInfoList(this List<SegmentMetadata> segmentMetadatas)
        {
            if (segmentMetadatas == null) return new List<EntityMetadata.SegmentInfo>();

            return segmentMetadatas.Select(sm => new EntityMetadata.SegmentInfo
            {
                FileName = sm.FileName,
                RecordCount = sm.RecordCount,
                SizeMB = sm.FileSizeBytes / (1024 * 1024), // Bytes to MB
                Checksum = sm.Checksum,
                CreatedAt = sm.CreatedAt,
                LastModified = sm.LastModified,
                IsActive = sm.IsActive,
                IsCompressed = sm.IsCompressed,
                CompressionRatio = sm.CompressionRatio,
                IdRange = new EntityMetadata.IdRange { Min = 0, Max = (int)sm.RecordCount }
            }).ToList();
        }

        /// <summary>
        /// List<EntityMetadata.SegmentInfo> to List<SegmentMetadata> conversion
        /// </summary>
        public static List<SegmentMetadata> ToSegmentMetadataList(this List<EntityMetadata.SegmentInfo> segmentInfos)
        {
            if (segmentInfos == null) return new List<SegmentMetadata>();

            return segmentInfos.Select((si, index) => new SegmentMetadata
            {
                SegmentId = index + 1,
                FileName = si.FileName,
                RecordCount = si.RecordCount,
                FileSizeBytes = si.SizeMB * 1024 * 1024, // MB to Bytes
                Checksum = si.Checksum,
                CreatedAt = si.CreatedAt,
                LastModified = si.LastModified,
                IsActive = si.IsActive,
                IsCompressed = si.IsCompressed,
                CompressionRatio = si.CompressionRatio
            }).ToList();
        }

        /// <summary>
        /// SegmentMetadata to EntityMetadata.SegmentInfo conversion
        /// </summary>
        public static EntityMetadata.SegmentInfo ToSegmentInfo(this SegmentMetadata segmentMetadata)
        {
            if (segmentMetadata == null) return null;

            return new EntityMetadata.SegmentInfo
            {
                FileName = segmentMetadata.FileName,
                RecordCount = segmentMetadata.RecordCount,
                SizeMB = segmentMetadata.FileSizeBytes / (1024 * 1024),
                Checksum = segmentMetadata.Checksum,
                CreatedAt = segmentMetadata.CreatedAt,
                LastModified = segmentMetadata.LastModified,
                IsActive = segmentMetadata.IsActive,
                IsCompressed = segmentMetadata.IsCompressed,
                CompressionRatio = segmentMetadata.CompressionRatio,
                IdRange = new EntityMetadata.IdRange { Min = 0, Max = (int)segmentMetadata.RecordCount }
            };
        }

        /// <summary>
        /// EntityMetadata.SegmentInfo to SegmentMetadata conversion
        /// </summary>
        public static SegmentMetadata ToSegmentMetadata(this EntityMetadata.SegmentInfo segmentInfo, int segmentId = 0)
        {
            if (segmentInfo == null) return null;

            return new SegmentMetadata
            {
                SegmentId = segmentId,
                FileName = segmentInfo.FileName,
                RecordCount = segmentInfo.RecordCount,
                FileSizeBytes = segmentInfo.SizeMB * 1024 * 1024,
                Checksum = segmentInfo.Checksum,
                CreatedAt = segmentInfo.CreatedAt,
                LastModified = segmentInfo.LastModified,
                IsActive = segmentInfo.IsActive,
                IsCompressed = segmentInfo.IsCompressed,
                CompressionRatio = segmentInfo.CompressionRatio
            };
        }
    }
}