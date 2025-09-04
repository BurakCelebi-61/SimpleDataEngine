using SimpleDataEngine.Core;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository interface with transaction support
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public interface ITransactionalRepository<T> : ICacheableRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Begins a transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// Indicates if a transaction is currently active
        /// </summary>
        bool IsInTransaction { get; }

        /// <summary>
        /// Executes an action within a transaction
        /// </summary>
        /// <param name="action">Action to execute</param>
        void ExecuteInTransaction(Action action);

        /// <summary>
        /// Executes an async action within a transaction
        /// </summary>
        /// <param name="action">Async action to execute</param>
        Task ExecuteInTransactionAsync(Func<Task> action);
    }
}