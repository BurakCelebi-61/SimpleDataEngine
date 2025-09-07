using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical.Extensions
{
    /// <summary>
    /// EntityIndex extension methods - top-level static class
    /// </summary>
    public static class EntityIndexExtensions
    {
        public static long GetTotalRecords(this EntityIndex entityIndex)
        {
            if (entityIndex?.PropertyIndexes == null) return 0;

            return entityIndex.PropertyIndexes.Values
                .SelectMany(pi => pi.Values)
                .SelectMany(info => info.SampleValues)
                .Distinct()
                .Count();
        }

        public static void SetTotalRecords(this EntityIndex entityIndex, long totalRecords)
        {
            if (entityIndex != null)
            {
                entityIndex.TotalRecords = totalRecords;
            }
        }

        public static DateTime GetCreatedAt(this EntityIndex entityIndex)
        {
            return entityIndex?.CreatedAt ?? DateTime.MinValue;
        }

        public static DateTime GetLastModified(this EntityIndex entityIndex)
        {
            return entityIndex?.LastModified ?? DateTime.MinValue;
        }

        public static string GetEntityName(this EntityIndex entityIndex)
        {
            return entityIndex?.EntityName ?? string.Empty;
        }
    }

    /// <summary>
    /// SegmentMetadata extension methods - top-level static class
    /// </summary>
    public static class SegmentMetadataExtensions
    {
        public static int GetSegmentId(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.SegmentId ?? 0;
        }

        public static string GetFileName(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.FileName ?? string.Empty;
        }

        public static DateTime GetCreatedAt(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.CreatedAt ?? DateTime.MinValue;
        }

        public static DateTime GetLastModified(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.LastModified ?? DateTime.MinValue;
        }

        public static bool GetIsActive(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.IsActive ?? false;
        }

        public static long GetFileSizeBytes(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.FileSizeBytes ?? 0;
        }

        public static string GetChecksum(this SegmentMetadata segmentMetadata)
        {
            return segmentMetadata?.Checksum ?? string.Empty;
        }
    }

    /// <summary>
    /// SegmentData extension methods - top-level static class
    /// </summary>
    public static class SegmentDataExtensions
    {
        public static int GetSegmentId<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.SegmentId ?? 0;
        }

        public static DateTime GetCreatedAt<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.CreatedAt ?? DateTime.MinValue;
        }

        public static DateTime GetLastModified<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.LastModified ?? DateTime.MinValue;
        }

        public static List<T> GetRecords<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.Records ?? new List<T>();
        }

        public static void SetLastModified<T>(this SegmentData<T> segmentData, DateTime lastModified) where T : class
        {
            if (segmentData != null)
            {
                segmentData.LastModified = lastModified;
            }
        }
    }

    /// <summary>
    /// EntityMetadata extension methods - top-level static class
    /// </summary>
    public static class EntityMetadataExtensions
    {
        public static string GetEntityName(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.EntityName ?? string.Empty;
        }

        public static List<SegmentMetadata> GetSegments(this EntityMetadata entityMetadata)
        {
            // EntityMetadata.Segments'i SegmentMetadata listesine dönüştür
            return entityMetadata?.Segments?.Select(si => new SegmentMetadata
            {
                SegmentId = 0, // Will be set elsewhere
                FileName = si.FileName,
                RecordCount = si.RecordCount,
                FileSizeBytes = si.SizeMB * 1024 * 1024,
                Checksum = si.Checksum,
                CreatedAt = si.CreatedAt,
                LastModified = si.LastModified,
                IsActive = si.IsActive,
                IsCompressed = si.IsCompressed,
                CompressionRatio = si.CompressionRatio
            }).ToList() ?? new List<SegmentMetadata>();
        }

        public static long CalculateTotalRecords(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Sum(s => s.RecordCount) ?? 0;
        }

        public static long CalculateTotalSizeBytes(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Sum(s => s.SizeMB * 1024 * 1024) ?? 0;
        }
    }
}