using SimpleDataEngine.Core;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository interface with caching capabilities
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface ICacheableRepository<T> : IEventRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Clears the repository cache
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <returns>Cache hit/miss statistics</returns>
        RepositoryCacheStats GetCacheStats();

        /// <summary>
        /// Enables or disables caching
        /// </summary>
        /// <param name="enabled">Whether to enable caching</param>
        void SetCacheEnabled(bool enabled);
    }
}