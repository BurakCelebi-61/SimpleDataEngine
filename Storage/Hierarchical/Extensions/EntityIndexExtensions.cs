using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical.Extensions
{
    /// <summary>
    /// EntityIndex extension methods
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
                .SelectMany(pi => pi.Values)
                .SelectMany(info => info.SampleValues)
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

        /// <summary>
        /// Entries güvenli erişim - Dictionary<string, Dictionary<string, PropertyIndexInfo>> için
        /// </summary>
        public static Dictionary<string, List<object>> GetEntries(this Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>> propertyIndexes)
        {
            if (propertyIndexes == null) return new Dictionary<string, List<object>>();

            var result = new Dictionary<string, List<object>>();
            foreach (var kvp in propertyIndexes)
            {
                var sampleValues = new List<object>();
                foreach (var infoKvp in kvp.Value)
                {
                    if (infoKvp.Value.SampleValues != null)
                    {
                        sampleValues.AddRange(infoKvp.Value.SampleValues);
                    }
                }
                result[kvp.Key] = sampleValues;
            }
            return result;
        }

        /// <summary>
        /// LastModified güvenli erişim - Dictionary<string, Dictionary<string, PropertyIndexInfo>> için
        /// </summary>
        public static DateTime GetLastModified(this Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var indexDict))
            {
                var latestUpdate = indexDict.Values
                    .Where(info => info != null)
                    .Select(info => info.LastUpdated)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
                return latestUpdate;
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// PropertyType güvenli erişim - Dictionary<string, Dictionary<string, PropertyIndexInfo>> için
        /// </summary>
        public static string GetPropertyType(this Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var indexDict))
            {
                // İlk PropertyIndexInfo'dan type bilgisini al
                var firstInfo = indexDict.Values.FirstOrDefault();
                return firstInfo != null ? "object" : string.Empty; // PropertyIndexInfo'da PropertyType field'ı yok, generic olarak "object" döndür
            }
            return string.Empty;
        }

        /// <summary>
        /// Property index statistics alma
        /// </summary>
        public static Dictionary<string, int> GetPropertyIndexCounts(this EntityIndex entityIndex)
        {
            var result = new Dictionary<string, int>();

            if (entityIndex?.PropertyIndexes != null)
            {
                foreach (var kvp in entityIndex.PropertyIndexes)
                {
                    var totalSamples = kvp.Value.Values
                        .Where(info => info.SampleValues != null)
                        .Sum(info => info.SampleValues.Count);
                    result[kvp.Key] = totalSamples;
                }
            }

            return result;
        }

        /// <summary>
        /// Entity index'i validate et
        /// </summary>
        public static bool IsValid(this EntityIndex entityIndex)
        {
            return entityIndex != null &&
                   !string.IsNullOrEmpty(entityIndex.EntityName) &&
                   entityIndex.PropertyIndexes != null;
        }

        /// <summary>
        /// Property index var mı kontrol et
        /// </summary>
        public static bool HasPropertyIndex(this EntityIndex entityIndex, string propertyName)
        {
            return entityIndex?.PropertyIndexes?.ContainsKey(propertyName) == true;
        }

        /// <summary>
        /// Tüm indexed property'lerin isimlerini al
        /// </summary>
        public static List<string> GetIndexedPropertyNames(this EntityIndex entityIndex)
        {
            return entityIndex?.PropertyIndexes?.Keys.ToList() ?? new List<string>();
        }
    }
}