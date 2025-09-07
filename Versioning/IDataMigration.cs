namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Interface for data migration operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IDataMigration<T> where T : class
    {
        /// <summary>
        /// Source version for this migration
        /// </summary>
        DataVersion FromVersion { get; }

        /// <summary>
        /// Target version for this migration
        /// </summary>
        DataVersion ToVersion { get; }

        /// <summary>
        /// Migration name/description
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Migration description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this migration is reversible
        /// </summary>
        bool IsReversible { get; }

        /// <summary>
        /// Executes the migration
        /// </summary>
        /// <param name="data">Data to migrate</param>
        /// <param name="context">Migration context</param>
        /// <returns>Migrated data</returns>
        Task<MigrationResult<T>> MigrateAsync(List<T> data, MigrationContext context);

        /// <summary>
        /// Reverses the migration (if supported)
        /// </summary>
        /// <param name="data">Data to reverse</param>
        /// <param name="context">Migration context</param>
        /// <returns>Reversed data</returns>
        Task<MigrationResult<T>> ReverseAsync(List<T> data, MigrationContext context);

        /// <summary>
        /// Validates that the migration can be applied
        /// </summary>
        /// <param name="data">Data to validate</param>
        /// <param name="context">Migration context</param>
        /// <returns>Validation result</returns>
        Task<MigrationValidationResult> ValidateAsync(List<T> data, MigrationContext context);

        /// <summary>
        /// Creates a backup before migration
        /// </summary>
        /// <param name="data">Data to backup</param>
        /// <param name="backupPath">Backup file path</param>
        /// <returns>True if backup was successful</returns>
        Task<bool> CreateBackupAsync(List<T> data, string backupPath);
    }
}