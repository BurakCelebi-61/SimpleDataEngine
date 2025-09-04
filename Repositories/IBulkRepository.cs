using SimpleDataEngine.Core;
using System.Linq.Expressions;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository interface with bulk operations
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface IBulkRepository<T> : IAsyncRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Adds multiple entities
        /// </summary>
        /// <param name="entities">Entities to add</param>
        void AddRange(IEnumerable<T> entities);

        /// <summary>
        /// Asynchronously adds multiple entities
        /// </summary>
        /// <param name="entities">Entities to add</param>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Updates multiple entities
        /// </summary>
        /// <param name="entities">Entities to update</param>
        /// <returns>Number of entities updated</returns>
        int UpdateRange(IEnumerable<T> entities);

        /// <summary>
        /// Asynchronously updates multiple entities
        /// </summary>
        /// <param name="entities">Entities to update</param>
        /// <returns>Number of entities updated</returns>
        Task<int> UpdateRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Deletes entities matching the predicate
        /// </summary>
        /// <param name="predicate">Delete predicate</param>
        /// <returns>Number of entities deleted</returns>
        int DeleteWhere(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Asynchronously deletes entities matching the predicate
        /// </summary>
        /// <param name="predicate">Delete predicate</param>
        /// <returns>Number of entities deleted</returns>
        Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Deletes multiple entities by their IDs
        /// </summary>
        /// <param name="ids">Entity IDs to delete</param>
        /// <returns>Number of entities deleted</returns>
        int DeleteRange(IEnumerable<int> ids);

        /// <summary>
        /// Asynchronously deletes multiple entities by their IDs
        /// </summary>
        /// <param name="ids">Entity IDs to delete</param>
        /// <returns>Number of entities deleted</returns>
        Task<int> DeleteRangeAsync(IEnumerable<int> ids);
    }
}