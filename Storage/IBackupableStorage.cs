namespace SimpleDataEngine.Storage
{
    /// <summary>
    /// Storage interface with backup and restore capabilities
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IBackupableStorage<T> : ITransactionalStorage<T> where T : class
    {
        /// <summary>
        /// Creates a backup of the storage
        /// </summary>
        /// <param name="backupPath">Path for the backup</param>
        void CreateBackup(string backupPath);

        /// <summary>
        /// Restores storage from a backup
        /// </summary>
        /// <param name="backupPath">Path of the backup to restore</param>
        void RestoreFromBackup(string backupPath);

        /// <summary>
        /// Gets the last backup timestamp
        /// </summary>
        DateTime? LastBackupTime { get; }
    }
}