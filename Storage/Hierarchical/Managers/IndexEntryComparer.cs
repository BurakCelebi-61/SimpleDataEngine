using SimpleDataEngine.Storage.Hierarchical.Models;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Index entry comparer for intersection operations
    /// </summary>
    public class IndexEntryComparer : IEqualityComparer<IndexEntry>
    {
        public bool Equals(IndexEntry x, IndexEntry y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            return Equals(x.RecordId, y.RecordId) && x.SegmentId == y.SegmentId;
        }

        public int GetHashCode(IndexEntry obj)
        {
            return HashCode.Combine(obj.RecordId, obj.SegmentId);
        }
    }
}