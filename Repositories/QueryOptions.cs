using SimpleDataEngine.Core;
using System.Linq.Expressions;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository query options
    /// </summary>
    public class QueryOptions<T> where T : class, IEntity
    {
        public Expression<Func<T, bool>> Filter { get; set; }
        public Expression<Func<T, object>> OrderBy { get; set; }
        public bool OrderDescending { get; set; } = false;
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = int.MaxValue;
        public List<Expression<Func<T, object>>> Includes { get; set; } = new();
        public bool UseCache { get; set; } = true;
        public TimeSpan? CacheExpiry { get; set; }
    }
}