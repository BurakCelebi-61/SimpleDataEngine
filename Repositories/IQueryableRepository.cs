using SimpleDataEngine.Core;
using System.Linq.Expressions;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Extended repository interface with query capabilities
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface IQueryableRepository<T> : IRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Finds entities matching the predicate
        /// </summary>
        /// <param name="predicate">Search predicate</param>
        /// <returns>List of matching entities</returns>
        List<T> Find(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Finds first entity matching the predicate
        /// </summary>
        /// <param name="predicate">Search predicate</param>
        /// <returns>First matching entity or null</returns>
        T FindFirst(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Counts entities matching the predicate
        /// </summary>
        /// <param name="predicate">Count predicate (null for all)</param>
        /// <returns>Count of matching entities</returns>
        int Count(Expression<Func<T, bool>> predicate = null);

        /// <summary>
        /// Checks if any entities match the predicate
        /// </summary>
        /// <param name="predicate">Search predicate (null for any)</param>
        /// <returns>True if any entities match</returns>
        bool Exists(Expression<Func<T, bool>> predicate = null);
    }
}