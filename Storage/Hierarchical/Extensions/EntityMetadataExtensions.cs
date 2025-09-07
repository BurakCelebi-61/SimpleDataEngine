using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical.Extensions
{
    /// <summary>
    /// EntityMetadata extension methods
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
        /// Segments güvenli erişim - SegmentMetadata listesine dönüştürme
        /// </summary>
        public static List<SegmentMetadata> GetSegments(this EntityMetadata entityMetadata)
        {
            if (entityMetadata?.Segments == null) return new List<SegmentMetadata>();

            // EntityMetadata.SegmentInfo'dan SegmentMetadata'ya dönüştür
            return entityMetadata.Segments.Select((si, index) => new SegmentMetadata
            {
                SegmentId = index + 1, // Auto-assign segment ID
                FileName = si.FileName,
                RecordCount = si.RecordCount,
                FileSizeBytes = si.FileSizeBytes, // Bu property artık var
                Checksum = si.Checksum,
                CreatedAt = si.CreatedAt,
                LastModified = si.LastModified,
                IsActive = si.IsActive,
                IsCompressed = si.IsCompressed,
                CompressionRatio = si.CompressionRatio,
                IsEncrypted = si.IsEncrypted,
                Statistics = si.Statistics,
                DeletedIds = si.DeletedIds
            }).ToList();
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

        /// <summary>
        /// EntityType güvenli erişim
        /// </summary>
        public static string GetEntityType(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.EntityType ?? string.Empty;
        }

        /// <summary>
        /// CurrentSegment güvenli erişim
        /// </summary>
        public static int GetCurrentSegment(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.CurrentSegment ?? 1;
        }

        /// <summary>
        /// TotalSizeMB güvenli erişim
        /// </summary>
        public static long GetTotalSizeMB(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.TotalSizeMB ?? 0;
        }

        /// <summary>
        /// CreatedAt güvenli erişim
        /// </summary>
        public static DateTime GetCreatedAt(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.CreatedAt ?? DateTime.MinValue;
        }

        /// <summary>
        /// LastModified güvenli erişim
        /// </summary>
        public static DateTime GetLastModified(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.LastModified ?? DateTime.MinValue;
        }

        /// <summary>
        /// Active segments alma
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetActiveSegments(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Where(s => s.IsActive).ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Inactive segments alma
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetInactiveSegments(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Where(s => !s.IsActive).ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Compressed segments alma
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetCompressedSegments(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Where(s => s.IsCompressed).ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Segment count alma
        /// </summary>
        public static int GetSegmentCount(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Count ?? 0;
        }

        /// <summary>
        /// Active segment count alma
        /// </summary>
        public static int GetActiveSegmentCount(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Segments?.Count(s => s.IsActive) ?? 0;
        }

        /// <summary>
        /// Average segment size in MB
        /// </summary>
        public static double GetAverageSegmentSizeMB(this EntityMetadata entityMetadata)
        {
            var segments = entityMetadata?.Segments;
            if (segments == null || !segments.Any()) return 0;

            return segments.Average(s => s.SizeMB);
        }

        /// <summary>
        /// Largest segment size in MB
        /// </summary>
        public static long GetLargestSegmentSizeMB(this EntityMetadata entityMetadata)
        {
            var segments = entityMetadata?.Segments;
            if (segments == null || !segments.Any()) return 0;

            return segments.Max(s => s.SizeMB);
        }

        /// <summary>
        /// Smallest segment size in MB
        /// </summary>
        public static long GetSmallestSegmentSizeMB(this EntityMetadata entityMetadata)
        {
            var segments = entityMetadata?.Segments;
            if (segments == null || !segments.Any()) return 0;

            return segments.Min(s => s.SizeMB);
        }

        /// <summary>
        /// Schema güvenli erişim
        /// </summary>
        public static EntityMetadata.EntitySchema GetSchema(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Schema ?? new EntityMetadata.EntitySchema();
        }

        /// <summary>
        /// Schema version alma
        /// </summary>
        public static string GetSchemaVersion(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Schema?.Version ?? "1.0.0";
        }

        /// <summary>
        /// Schema properties alma
        /// </summary>
        public static List<EntityMetadata.EntitySchema.PropertyInfo> GetSchemaProperties(this EntityMetadata entityMetadata)
        {
            return entityMetadata?.Schema?.Properties ?? new List<EntityMetadata.EntitySchema.PropertyInfo>();
        }

        /// <summary>
        /// Add segment güvenli ekleme
        /// </summary>
        public static void AddSegmentSafe(this EntityMetadata entityMetadata, EntityMetadata.SegmentInfo segmentInfo)
        {
            if (entityMetadata != null && segmentInfo != null)
            {
                entityMetadata.Segments ??= new List<EntityMetadata.SegmentInfo>();
                entityMetadata.Segments.Add(segmentInfo);
                entityMetadata.RecalculateStatistics();
            }
        }

        /// <summary>
        /// Remove segment güvenli silme
        /// </summary>
        public static bool RemoveSegmentSafe(this EntityMetadata entityMetadata, string fileName)
        {
            if (entityMetadata?.Segments != null)
            {
                var segment = entityMetadata.Segments.FirstOrDefault(s => s.FileName == fileName);
                if (segment != null)
                {
                    entityMetadata.Segments.Remove(segment);
                    entityMetadata.RecalculateStatistics();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Update last modified timestamp
        /// </summary>
        public static void Touch(this EntityMetadata entityMetadata)
        {
            if (entityMetadata != null)
            {
                entityMetadata.LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Entity metadata validate
        /// </summary>
        public static bool IsValid(this EntityMetadata entityMetadata)
        {
            return entityMetadata != null &&
                   !string.IsNullOrEmpty(entityMetadata.EntityName) &&
                   entityMetadata.Segments != null &&
                   entityMetadata.CreatedAt != DateTime.MinValue;
        }

        /// <summary>
        /// Get entity statistics summary
        /// </summary>
        public static string GetSummary(this EntityMetadata entityMetadata)
        {
            if (entityMetadata == null) return "No entity metadata";

            return $"Entity '{entityMetadata.EntityName}': " +
                   $"{entityMetadata.TotalRecords} records, " +
                   $"{entityMetadata.GetSegmentCount()} segments, " +
                   $"{entityMetadata.TotalSizeMB} MB, " +
                   $"Active: {entityMetadata.GetActiveSegmentCount()}/{entityMetadata.GetSegmentCount()}";
        }

        /// <summary>
        /// Get entity age in days
        /// </summary>
        public static int GetAgeInDays(this EntityMetadata entityMetadata)
        {
            if (entityMetadata == null) return 0;
            return (DateTime.UtcNow - entityMetadata.CreatedAt).Days;
        }

        /// <summary>
        /// Son güncellenmesinden bu yana geçen gün sayısı
        /// </summary>
        public static int GetDaysSinceLastModified(this EntityMetadata entityMetadata)
        {
            if (entityMetadata == null) return 0;
            return (DateTime.UtcNow - entityMetadata.LastModified).Days;
        }

        /// <summary>
        /// Segment by filename alma
        /// </summary>
        public static EntityMetadata.SegmentInfo GetSegmentByFileName(this EntityMetadata entityMetadata, string fileName)
        {
            return entityMetadata?.Segments?.FirstOrDefault(s => s.FileName == fileName);
        }

        /// <summary>
        /// Segment by record count range alma
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetSegmentsByRecordCountRange(this EntityMetadata entityMetadata, long minRecords, long maxRecords)
        {
            return entityMetadata?.Segments?
                .Where(s => s.RecordCount >= minRecords && s.RecordCount <= maxRecords)
                .ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Segment by size range alma (MB)
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetSegmentsBySizeRange(this EntityMetadata entityMetadata, long minSizeMB, long maxSizeMB)
        {
            return entityMetadata?.Segments?
                .Where(s => s.SizeMB >= minSizeMB && s.SizeMB <= maxSizeMB)
                .ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Old segments alma (belirli günden eski)
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetOldSegments(this EntityMetadata entityMetadata, int olderThanDays)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            return entityMetadata?.Segments?
                .Where(s => s.CreatedAt < cutoffDate)
                .ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Recent segments alma (belirli günden yeni)
        /// </summary>
        public static List<EntityMetadata.SegmentInfo> GetRecentSegments(this EntityMetadata entityMetadata, int newerThanDays)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-newerThanDays);
            return entityMetadata?.Segments?
                .Where(s => s.CreatedAt >= cutoffDate)
                .ToList() ?? new List<EntityMetadata.SegmentInfo>();
        }

        /// <summary>
        /// Fragmentation ratio hesaplama (0-1 arası, 1 = çok fragmente)
        /// </summary>
        public static double GetFragmentationRatio(this EntityMetadata entityMetadata)
        {
            var segments = entityMetadata?.Segments;
            if (segments == null || segments.Count <= 1) return 0;

            var avgSize = segments.Average(s => s.SizeMB);
            var variance = segments.Average(s => Math.Pow(s.SizeMB - avgSize, 2));
            var stdDev = Math.Sqrt(variance);

            return avgSize > 0 ? Math.Min(1.0, stdDev / avgSize) : 0;
        }

        /// <summary>
        /// Compression efficiency hesaplama
        /// </summary>
        public static double GetCompressionEfficiency(this EntityMetadata entityMetadata)
        {
            var compressedSegments = entityMetadata?.GetCompressedSegments();
            if (compressedSegments == null || !compressedSegments.Any()) return 0;

            return compressedSegments.Average(s => s.CompressionRatio);
        }

        /// <summary>
        /// Storage optimization suggestions
        /// </summary>
        public static List<string> GetOptimizationSuggestions(this EntityMetadata entityMetadata)
        {
            var suggestions = new List<string>();

            if (entityMetadata == null) return suggestions;

            // Fragmentation check
            if (entityMetadata.GetFragmentationRatio() > 0.5)
            {
                suggestions.Add("High fragmentation detected. Consider segment compaction.");
            }

            // Small segments check
            var smallSegments = entityMetadata.GetSegmentsBySizeRange(0, 10); // < 10MB
            if (smallSegments.Count > 3)
            {
                suggestions.Add($"Found {smallSegments.Count} small segments. Consider merging.");
            }

            // Old segments check
            var oldSegments = entityMetadata.GetOldSegments(90); // > 90 days
            if (oldSegments.Count > 0)
            {
                suggestions.Add($"Found {oldSegments.Count} old segments. Consider archiving.");
            }

            // Compression check
            var uncompressedSegments = entityMetadata.Segments?.Where(s => !s.IsCompressed).ToList();
            if (uncompressedSegments?.Count > 0)
            {
                suggestions.Add($"Found {uncompressedSegments.Count} uncompressed segments. Consider enabling compression.");
            }

            return suggestions;
        }
    }
}