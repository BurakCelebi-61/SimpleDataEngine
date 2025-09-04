namespace SimpleDataEngine.Core
{
    /// <summary>
    /// Base interface for all entities in SimpleDataEngine
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        DateTime UpdateTime { get; set; }
    }
}