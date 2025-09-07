namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// EntityIndex ve EntityMetadata için eksik property'lerin tamamlanması
    /// </summary>
    public static class MissingPropertiesFix
    {
        /// <summary>
        /// EntityIndex'e eksik property'leri ekleyen extension methods
        /// </summary>
        public static class EntityIndexExtensions
        {
            /// <summary>
            /// TotalRecords property erişimi - hatalarda eksik olan
            /// </summary>
            public static long GetTotalRecords(this EntityIndex entityIndex)
            {
                if (entityIndex?.PropertyIndexes == null) return 0;

                return entityIndex.PropertyIndexes.Values
                    .SelectMany(pi => pi.Entries)
                    .Select(e => e.RecordId)
                    .Distinct()
                    .Count();
            }

            /// <summary>
            /// TotalRecords property set etme
            /// </summary>
            public static void SetTotalRecords(this EntityIndex entityIndex, long totalRecords)
            {
                if (entityIndex != null)
                {
                    entityIndex.TotalRecords = totalRecords;
                }
            }

            /// <summary>
            /// CreatedAt property güvenli erişim
            /// </summary>
            public static DateTime GetCreatedAt(this EntityIndex entityIndex)
            {
                return entityIndex?.CreatedAt ?? DateTime.MinValue;
            }

            /// <summary>
            /// LastModified property güvenli erişim
            /// </summary>
            public static DateTime GetLastModified(this EntityIndex entityIndex)
            {
                return entityIndex?.LastModified ?? DateTime.MinValue;
            }

            /// <summary>
            /// EntityName property güvenli erişim
            /// </summary>
            public static string GetEntityName(this EntityIndex entityIndex)
            {
                return entityIndex?.EntityName ?? string.Empty;
            }
        }

        /// <summary>
        /// SegmentMetadata için eksik property erişim methodları
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
        }

        /// <summary>
        /// SegmentData<T> için eksik property erişim methodları
        /// </summary>
        public static class SegmentDataExtensions
        {
            /// <summary>
            /// SegmentId güvenli erişim
            /// </summary>
            public static int GetSegmentId<T>(this SegmentData<T> segmentData) where T : class
            {
                return segmentData?.SegmentId ?? 0;
            }

            /// <summary>
            /// CreatedAt güvenli erişim
            /// </summary>
            public static DateTime GetCreatedAt<T>(this SegmentData<T> segmentData) where T : class
            {
                return segmentData?.CreatedAt ?? DateTime.MinValue;
            }

            /// <summary>
            /// LastModified güvenli erişim
            /// </summary>
            public static DateTime GetLastModified<T>(this SegmentData<T> segmentData) where T : class
            {
                return segmentData?.LastModified ?? DateTime.MinValue;
            }

            /// <summary>
            /// Records güvenli erişim
            /// </summary>
            public static List<T> GetRecords<T>(this SegmentData<T> segmentData) where T : class
            {
                return segmentData?.Records ?? new List<T>();
            }

            /// <summary>
            /// LastModified set etme
            /// </summary>
            public static void SetLastModified<T>(this SegmentData<T> segmentData, DateTime lastModified) where T : class
            {
                if (segmentData != null)
                {
                    segmentData.LastModified = lastModified;
                }
            }
        }

        /// <summary>
        /// EntityMetadata için eksik property erişim methodları
        /// </summary>
        public static class EntityMetadataExtensions
        {
            /// <summary>
            /// EntityName güvenli erişim
            /// </summary>
            public static string GetEntityName(this EntityMetadata entityMetadata)
            {
                return entityMetadata?.EntityName ?? string.Empty;
            }

            /// <summary>
            /// Segments güvenli erişim
            /// </summary>
            public static List<SegmentMetadata> GetSegments(this EntityMetadata entityMetadata)
            {
                return entityMetadata?.Segments ?? new List<SegmentMetadata>();
            }

            /// <summary>
            /// TotalRecords hesaplama
            /// </summary>
            public static long CalculateTotalRecords(this EntityMetadata entityMetadata)
            {
                return entityMetadata?.Segments?.Sum(s => s.RecordCount) ?? 0;
            }

            /// <summary>
            /// TotalSizeBytes hesaplama
            /// </summary>
            public static long CalculateTotalSizeBytes(this EntityMetadata entityMetadata)
            {
                return entityMetadata?.Segments?.Sum(s => s.FileSizeBytes) ?? 0;
            }
        }
    }
}