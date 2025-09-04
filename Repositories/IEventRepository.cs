using SimpleDataEngine.Core;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository interface with events
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface IEventRepository<T> : IBulkRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Event fired when an entity is added
        /// </summary>
        event Action<T> EntityAdded;

        /// <summary>
        /// Event fired when an entity is updated
        /// </summary>
        event Action<T> EntityUpdated;

        /// <summary>
        /// Event fired when an entity is deleted
        /// </summary>
        event Action<T> EntityDeleted;

        /// <summary>
        /// Event fired before saving changes
        /// </summary>
        event Action BeforeSave;

        /// <summary>
        /// Event fired after saving changes
        /// </summary>
        event Action AfterSave;
    }
}