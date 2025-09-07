namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Property-specific index containing all entries for a property
    /// </summary>
    public class PropertyIndex
    {
        /// <summary>
        /// Property name being indexed
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Data type of the property
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// All index entries for this property
        /// </summary>
        public List<IndexEntry> Entries { get; set; } = new List<IndexEntry>();

        /// <summary>
        /// When this property index was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this property index was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Index type (Range, Hash, Unique, etc.)
        /// </summary>
        public IndexType IndexType { get; set; } = IndexType.Range;

        /// <summary>
        /// Whether this index is active and being maintained
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Index statistics
        /// </summary>
        public PropertyIndexStatistics Statistics { get; set; } = new PropertyIndexStatistics();

        public PropertyIndex()
        {
        }

        public PropertyIndex(string propertyName, string propertyType)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            CreatedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Add an index entry
        /// </summary>
        public void AddEntry(IndexEntry entry)
        {
            if (entry == null) return;

            entry.PropertyName = PropertyName;
            entry.PropertyType = PropertyType;
            Entries.Add(entry);
            LastModified = DateTime.UtcNow;
            Statistics.TotalEntries = Entries.Count;
            Statistics.UniqueValues = Entries.Select(e => e.Value).Distinct().Count();
        }

        /// <summary>
        /// Remove entries by record ID
        /// </summary>
        public int RemoveEntriesByRecordId(object recordId)
        {
            var removed = Entries.RemoveAll(e => Equals(e.RecordId, recordId));
            if (removed > 0)
            {
                LastModified = DateTime.UtcNow;
                Statistics.TotalEntries = Entries.Count;
                Statistics.UniqueValues = Entries.Select(e => e.Value).Distinct().Count();
            }
            return removed;
        }

        /// <summary>
        /// Find entries by value
        /// </summary>
        public List<IndexEntry> FindByValue(object value)
        {
            return Entries.Where(e => Equals(e.Value, value)).ToList();
        }

        /// <summary>
        /// Find entries by value range
        /// </summary>
        public List<IndexEntry> FindByRange(object minValue, object maxValue)
        {
            if (minValue is IComparable minComp && maxValue is IComparable maxComp)
            {
                return Entries.Where(e =>
                {
                    if (e.Value is IComparable valueComp)
                    {
                        return valueComp.CompareTo(minComp) >= 0 &&
                               valueComp.CompareTo(maxComp) <= 0;
                    }
                    return false;
                }).ToList();
            }
            return new List<IndexEntry>();
        }

        /// <summary>
        /// Get all unique values in this index
        /// </summary>
        public List<object> GetUniqueValues()
        {
            return Entries.Select(e => e.Value).Distinct().ToList();
        }

        /// <summary>
        /// Clear all entries
        /// </summary>
        public void Clear()
        {
            Entries.Clear();
            LastModified = DateTime.UtcNow;
            Statistics.TotalEntries = 0;
            Statistics.UniqueValues = 0;
        }

        public override string ToString()
        {
            return $"PropertyIndex[{PropertyName}] - {Entries.Count} entries";
        }
    }
}