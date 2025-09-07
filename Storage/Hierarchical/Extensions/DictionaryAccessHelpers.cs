using SimpleDataEngine.Storage.Hierarchical.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDataEngine.Storage.Hierarchical.Extensions
{
    /// <summary>
    /// Dictionary access helper extension methods for PropertyIndex operations
    /// </summary>
    public static class DictionaryAccessHelpers
    {
        /// <summary>
        /// Dictionary<string, PropertyIndex>'den Entries listesi güvenli erişim
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>Index entries list or empty list if not found</returns>
        public static List<IndexEntry> GetEntriesSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.Entries ?? new List<IndexEntry>();
            }
            return new List<IndexEntry>();
        }

        /// <summary>
        /// Dictionary<string, PropertyIndex>'den LastModified tarih güvenli erişim
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>Last modified date or DateTime.MinValue if not found</returns>
        public static DateTime GetLastModifiedSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.LastModified ?? DateTime.MinValue;
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// Dictionary<string, PropertyIndex>'den PropertyType güvenli erişim
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>Property type string or empty string if not found</returns>
        public static string GetPropertyTypeSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.PropertyType ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// PropertyIndex'e güvenli IndexEntry ekleme
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <param name="entry">Index entry to add</param>
        public static void AddEntrySafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key, IndexEntry entry)
        {
            if (propertyIndexes == null || entry == null || string.IsNullOrEmpty(key))
                return;

            if (!propertyIndexes.ContainsKey(key))
            {
                propertyIndexes[key] = new PropertyIndex(key, entry.PropertyType ?? typeof(object).Name);
            }

            propertyIndexes[key].AddEntry(entry);
        }

        /// <summary>
        /// PropertyIndex'den record ID'ye göre entry'leri güvenli silme
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <param name="recordId">Record ID to remove</param>
        /// <returns>Number of entries removed</returns>
        public static int RemoveEntriesSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key, object recordId)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.RemoveEntriesByRecordId(recordId) ?? 0;
            }
            return 0;
        }

        /// <summary>
        /// PropertyIndex'den value'ya göre entry'leri bulma
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <param name="value">Value to search for</param>
        /// <returns>List of matching index entries</returns>
        public static List<IndexEntry> FindByValueSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key, object value)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.FindByValue(value) ?? new List<IndexEntry>();
            }
            return new List<IndexEntry>();
        }

        /// <summary>
        /// PropertyIndex'den unique değerler listesi alma
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>List of unique values</returns>
        public static List<object> GetUniqueValuesSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.GetUniqueValues() ?? new List<object>();
            }
            return new List<object>();
        }

        /// <summary>
        /// PropertyIndex'de entry sayısı alma
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>Number of entries in the property index</returns>
        public static int GetEntryCountSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.Entries?.Count ?? 0;
            }
            return 0;
        }

        /// <summary>
        /// PropertyIndex var mı kontrolü
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>True if property index exists</returns>
        public static bool HasPropertyIndexSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            return propertyIndexes != null &&
                   !string.IsNullOrEmpty(key) &&
                   propertyIndexes.ContainsKey(key);
        }

        /// <summary>
        /// PropertyIndex'i tamamen temizleme
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        public static void ClearPropertyIndexSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                propertyIndex?.Clear();
            }
        }

        /// <summary>
        /// PropertyIndex'i dictionary'den kaldırma
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>True if property index was removed</returns>
        public static bool RemovePropertyIndexSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && !string.IsNullOrEmpty(key))
            {
                return propertyIndexes.Remove(key);
            }
            return false;
        }

        /// <summary>
        /// Tüm PropertyIndex'lerdeki toplam entry sayısı
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <returns>Total number of entries across all property indexes</returns>
        public static int GetTotalEntryCount(this Dictionary<string, PropertyIndex> propertyIndexes)
        {
            if (propertyIndexes == null) return 0;

            return propertyIndexes.Values
                .Where(pi => pi?.Entries != null)
                .Sum(pi => pi.Entries.Count);
        }

        /// <summary>
        /// PropertyIndex istatistiklerini alma
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <returns>Property index statistics or null if not found</returns>
        public static PropertyIndexStatistics GetStatisticsSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.Statistics;
            }
            return null;
        }

        /// <summary>
        /// PropertyIndex son güncelleme tarihini ayarlama
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <param name="lastModified">Last modified date</param>
        public static void SetLastModifiedSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key, DateTime lastModified)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                if (propertyIndex != null)
                {
                    propertyIndex.LastModified = lastModified;
                }
            }
        }

        /// <summary>
        /// Multiple entry'leri toplu ekleme
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <param name="entries">List of entries to add</param>
        public static void AddEntriesSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key, IEnumerable<IndexEntry> entries)
        {
            if (propertyIndexes == null || entries == null || string.IsNullOrEmpty(key))
                return;

            if (!propertyIndexes.ContainsKey(key))
            {
                var firstEntry = entries.FirstOrDefault();
                propertyIndexes[key] = new PropertyIndex(key, firstEntry?.PropertyType ?? typeof(object).Name);
            }

            var propertyIndex = propertyIndexes[key];
            foreach (var entry in entries)
            {
                if (entry != null)
                {
                    propertyIndex.AddEntry(entry);
                }
            }
        }

        /// <summary>
        /// Range query için helper method
        /// </summary>
        /// <param name="propertyIndexes">Property indexes dictionary</param>
        /// <param name="key">Property key</param>
        /// <param name="minValue">Minimum value</param>
        /// <param name="maxValue">Maximum value</param>
        /// <returns>List of entries within range</returns>
        public static List<IndexEntry> FindByRangeSafe(this Dictionary<string, PropertyIndex> propertyIndexes, string key, object minValue, object maxValue)
        {
            if (propertyIndexes != null && propertyIndexes.TryGetValue(key, out var propertyIndex))
            {
                return propertyIndex?.FindByRange(minValue, maxValue) ?? new List<IndexEntry>();
            }
            return new List<IndexEntry>();
        }
    }
}
