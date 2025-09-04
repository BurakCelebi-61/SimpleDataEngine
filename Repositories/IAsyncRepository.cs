using SimpleDataEngine.Core;
using System.Linq.Expressions;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository interface with async operations
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface IAsyncRepository<T> : IQueryableRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Asynchronously gets all entities
        /// </summary>
        /// <returns>List of all entities</returns>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Asynchronously gets entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Entity if found, null otherwise</returns>
        Task<T> GetByIdAsync(int id);

        /// <summary>
        /// Asynchronously adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        Task AddAsync(T entity);

        /// <summary>
        /// Asynchronously updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <returns>True if updated, false if not found</returns>
        Task<bool> UpdateAsync(T entity);

        /// <summary>
        /// Asynchronously deletes an entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Asynchronously saves all pending changes
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Asynchronously finds entities matching the predicate
        /// </summary>
        /// <param name="predicate">Search predicate</param>
        /// <returns>List of matching entities</returns>
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Asynchronously finds first entity matching the predicate
        /// </summary>
        /// <param name="predicate">Search predicate</param>
        /// <returns>First matching entity or null</returns>
        Task<T> FindFirstAsync(Expression<Func<T, bool>> predicate);
    }
}