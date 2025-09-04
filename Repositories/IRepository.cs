using SimpleDataEngine.Core;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Base repository interface for entity operations
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface IRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <returns>List of all entities</returns>
        List<T> GetAll();

        /// <summary>
        /// Gets entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Entity if found, null otherwise</returns>
        T GetById(int id);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        void Add(T entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <returns>True if updated, false if not found</returns>
        bool Update(T entity);

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>True if deleted, false if not found</returns>
        bool Delete(int id);

        /// <summary>
        /// Saves all pending changes
        /// </summary>
        void Save();
    }
}