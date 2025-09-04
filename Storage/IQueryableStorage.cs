using System.Linq.Expressions;

namespace SimpleDataEngine.Storage
{
    /// <summary>
    /// Extended storage interface with query capabilities
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IQueryableStorage<T> : IAsyncStorage<T> where T : class
    {
        /// <summary>
        /// Queries items based on predicate
        /// </summary>
        /// <param name="predicate">Query predicate</param>
        /// <returns>Filtered items</returns>
        List<T> Query(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Asynchronously queries items based on predicate
        /// </summary>
        /// <param name="predicate">Query predicate</param>
        /// <returns>Filtered items</returns>
        Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Counts items based on predicate
        /// </summary>
        /// <param name="predicate">Count predicate</param>
        /// <returns>Count of matching items</returns>
        int Count(Expression<Func<T, bool>> predicate = null);

        /// <summary>
        /// Checks if any items match the predicate
        /// </summary>
        /// <param name="predicate">Search predicate</param>
        /// <returns>True if any items match</returns>
        bool Any(Expression<Func<T, bool>> predicate = null);
    }
}