namespace SimpleDataEngine.Storage
{
    /// <summary>
    /// Storage interface with transaction support
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface ITransactionalStorage<T> : IQueryableStorage<T> where T : class
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
    }
}