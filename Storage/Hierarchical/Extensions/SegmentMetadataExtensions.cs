using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical.Extensions
{
    /// <summary>
    /// SegmentMetadata extension methods
    /// </summary>
    public static class SegmentMetadataExtensions
    {
        /// <summary>
        /// SegmentId güvenli erişim
        /// </summary>
        public static int GetSegmentId(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.SegmentId ?? 0;
        }

        /// <summary>
        /// FileName güvenli erişim
        /// </summary>
        public static string GetFileName(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.FileName ?? string.Empty;
        }

        /// <summary>
        /// CreatedAt güvenli erişim
        /// </summary>
        public static DateTime GetCreatedAt(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.CreatedAt ?? DateTime.MinValue;
        }

        /// <summary>
        /// LastModified güvenli erişim
        /// </summary>
        public static DateTime GetLastModified(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.LastModified ?? DateTime.MinValue;
        }

        /// <summary>
        /// IsActive güvenli erişim
        /// </summary>
        public static bool GetIsActive(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.IsActive ?? false;
        }

        /// <summary>
        /// FileSizeBytes güvenli erişim
        /// </summary>
        public static long GetFileSizeBytes(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.FileSizeBytes ?? 0;
        }

        /// <summary>
        /// Checksum güvenli erişim
        /// </summary>
        public static string GetChecksum(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.Checksum ?? string.Empty;
        }

        /// <summary>
        /// RecordCount güvenli erişim
        /// </summary>
        public static long GetRecordCount(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.RecordCount ?? 0;
        }

        /// <summary>
        /// Size in MB güvenli erişim
        /// </summary>
        public static double GetSizeInMB(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata != null ? segmentMetadata.FileSizeBytes / (1024.0 * 1024.0) : 0;
        }

        /// <summary>
        /// Size in KB güvenli erişim
        /// </summary>
        public static double GetSizeInKB(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata != null ? segmentMetadata.FileSizeBytes / 1024.0 : 0;
        }

        /// <summary>
        /// Compression ratio güvenli erişim
        /// </summary>
        public static double GetCompressionRatio(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.CompressionRatio ?? 0.0;
        }

        /// <summary>
        /// Is compressed kontrol
        /// </summary>
        public static bool IsCompressed(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.IsCompressed ?? false;
        }

        /// <summary>
        /// Is encrypted kontrol
        /// </summary>
        public static bool IsEncrypted(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.IsEncrypted ?? false;
        }

        /// <summary>
        /// Segment boş mu kontrol
        /// </summary>
        public static bool IsEmpty(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata == null || segmentMetadata.RecordCount == 0;
        }

        /// <summary>
        /// Segment büyük mü kontrol (threshold'a göre)
        /// </summary>
        public static bool IsLarge(this SegmentMetadata segmentMetadata, long thresholdBytes = 50 * 1024 * 1024) // 50MB default
        {
            return segmentMetadata != null && segmentMetadata.FileSizeBytes > thresholdBytes;
        }

        /// <summary>
        /// Formatted size string
        /// </summary>
        public static string GetFormattedSize(this SegmentMetadata segmentMetadata)
        {
            if (segmentMetadata == null) return "0 B";

            var bytes = segmentMetadata.FileSizeBytes;
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            else
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        /// <summary>
        /// Update last modified timestamp
        /// </summary>
        public static void TouchLastModified(this SegmentMetadata segmentMetadata)
        {
            if (segmentMetadata != null)
            {
                segmentMetadata.LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Segment age in days
        /// </summary>
        public static int GetAgeInDays(this SegmentMetadata segmentMetadata)
        {
            if (segmentMetadata == null) return 0;
            return (DateTime.UtcNow - segmentMetadata.CreatedAt).Days;
        }

        /// <summary>
        /// Segment'in son güncellenmesinden bu yana geçen gün sayısı
        /// </summary>
        public static int GetDaysSinceLastModified(this SegmentMetadata segmentMetadata)
        {
            if (segmentMetadata == null) return 0;
            return (DateTime.UtcNow - segmentMetadata.LastModified).Days;
        }

        /// <summary>
        /// Segment statistics summary
        /// </summary>
        public static string GetSummary(this SegmentMetadata segmentMetadata)
        {
            if (segmentMetadata == null) return "No segment metadata";

            return $"Segment {segmentMetadata.SegmentId}: {segmentMetadata.RecordCount} records, " +
                   $"{segmentMetadata.GetFormattedSize()}, " +
                   $"Age: {segmentMetadata.GetAgeInDays()} days, " +
                   $"Active: {segmentMetadata.IsActive}";
        }
    }
}