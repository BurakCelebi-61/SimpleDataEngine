using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical.Extensions
{
    /// <summary>
    /// SegmentData extension methods
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

        /// <summary>
        /// Record count güvenli erişim
        /// </summary>
        public static int GetRecordCount<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.Records?.Count ?? 0;
        }

        /// <summary>
        /// Segment boş mu kontrol
        /// </summary>
        public static bool IsEmpty<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData == null || segmentData.Records == null || segmentData.Records.Count == 0;
        }

        /// <summary>
        /// Segment dolu mu kontrol (maksimum kayıt sayısına göre)
        /// </summary>
        public static bool IsFull<T>(this SegmentData<T> segmentData, int maxRecords) where T : class
        {
            return segmentData != null && segmentData.Records != null && segmentData.Records.Count >= maxRecords;
        }

        /// <summary>
        /// Add record güvenli ekleme
        /// </summary>
        public static void AddRecordSafe<T>(this SegmentData<T> segmentData, T record) where T : class
        {
            if (segmentData != null && record != null)
            {
                segmentData.Records ??= new List<T>();
                segmentData.Records.Add(record);
                segmentData.LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Add multiple records güvenli ekleme
        /// </summary>
        public static void AddRecordsSafe<T>(this SegmentData<T> segmentData, IEnumerable<T> records) where T : class
        {
            if (segmentData != null && records != null)
            {
                segmentData.Records ??= new List<T>();
                segmentData.Records.AddRange(records.Where(r => r != null));
                segmentData.LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Remove record güvenli silme
        /// </summary>
        public static bool RemoveRecordSafe<T>(this SegmentData<T> segmentData, T record) where T : class
        {
            if (segmentData?.Records != null && record != null)
            {
                var removed = segmentData.Records.Remove(record);
                if (removed)
                {
                    segmentData.LastModified = DateTime.UtcNow;
                }
                return removed;
            }
            return false;
        }

        /// <summary>
        /// Clear all records güvenli temizleme
        /// </summary>
        public static void ClearRecordsSafe<T>(this SegmentData<T> segmentData) where T : class
        {
            if (segmentData?.Records != null)
            {
                segmentData.Records.Clear();
                segmentData.LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Get records by predicate
        /// </summary>
        public static List<T> GetRecordsWhere<T>(this SegmentData<T> segmentData, Func<T, bool> predicate) where T : class
        {
            if (segmentData?.Records == null || predicate == null)
                return new List<T>();

            return segmentData.Records.Where(predicate).ToList();
        }

        /// <summary>
        /// Update last modified timestamp
        /// </summary>
        public static void Touch<T>(this SegmentData<T> segmentData) where T : class
        {
            if (segmentData != null)
            {
                segmentData.LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Version güvenli erişim
        /// </summary>
        public static string GetVersion<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.Version ?? "1.0.0";
        }

        /// <summary>
        /// Checksum güvenli erişim
        /// </summary>
        public static string GetChecksum<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.Checksum ?? string.Empty;
        }

        /// <summary>
        /// Is compressed kontrol
        /// </summary>
        public static bool IsCompressed<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData?.IsCompressed ?? false;
        }

        /// <summary>
        /// Segment age in days
        /// </summary>
        public static int GetAgeInDays<T>(this SegmentData<T> segmentData) where T : class
        {
            if (segmentData == null) return 0;
            return (DateTime.UtcNow - segmentData.CreatedAt).Days;
        }

        /// <summary>
        /// Son güncellenmesinden bu yana geçen gün sayısı
        /// </summary>
        public static int GetDaysSinceLastModified<T>(this SegmentData<T> segmentData) where T : class
        {
            if (segmentData == null) return 0;
            return (DateTime.UtcNow - segmentData.LastModified).Days;
        }

        /// <summary>
        /// Segment metadata oluştur
        /// </summary>
        public static SegmentMetadata ToSegmentMetadata<T>(this SegmentData<T> segmentData) where T : class
        {
            if (segmentData == null) return null;

            return new SegmentMetadata
            {
                SegmentId = segmentData.SegmentId,
                RecordCount = segmentData.Records?.Count ?? 0,
                CreatedAt = segmentData.CreatedAt,
                LastModified = segmentData.LastModified,
                IsActive = true,
                IsCompressed = segmentData.IsCompressed,
                Checksum = segmentData.Checksum
            };
        }

        /// <summary>
        /// Segment statistics summary
        /// </summary>
        public static string GetSummary<T>(this SegmentData<T> segmentData) where T : class
        {
            if (segmentData == null) return "No segment data";

            return $"SegmentData<{typeof(T).Name}> {segmentData.SegmentId}: " +
                   $"{segmentData.Records?.Count ?? 0} records, " +
                   $"Age: {segmentData.GetAgeInDays()} days, " +
                   $"Version: {segmentData.GetVersion()}";
        }

        /// <summary>
        /// Validate segment data
        /// </summary>
        public static bool IsValid<T>(this SegmentData<T> segmentData) where T : class
        {
            return segmentData != null &&
                   segmentData.SegmentId > 0 &&
                   segmentData.Records != null &&
                   segmentData.CreatedAt != DateTime.MinValue;
        }
    }
}