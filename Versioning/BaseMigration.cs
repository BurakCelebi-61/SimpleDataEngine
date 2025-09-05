using SimpleDataEngine.Audit;
using System.Text.Json;

namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Base class for data migrations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public abstract class BaseMigration<T> : IDataMigration<T> where T : class
    {
        /// <inheritdoc />
        public abstract DataVersion FromVersion { get; }

        /// <inheritdoc />
        public abstract DataVersion ToVersion { get; }

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public virtual string Description => $"Migration from {FromVersion} to {ToVersion}";

        /// <inheritdoc />
        public virtual bool IsReversible => false;

        /// <inheritdoc />
        public async Task<MigrationResult<T>> MigrateAsync(List<T> data, MigrationContext context)
        {
            var result = new MigrationResult<T>
            {
                FromVersion = FromVersion,
                ToVersion = ToVersion,
                StartTime = DateTime.Now,
                OriginalData = new List<T>(data)
            };

            try
            {
                AuditLogger.Log("MIGRATION_STARTED", new
                {
                    MigrationName = Name,
                    FromVersion = FromVersion.ToString(),
                    ToVersion = ToVersion.ToString(),
                    ItemCount = data.Count,
                    DryRun = context.DryRun
                }, category: AuditCategory.Data);

                // Validate migration before execution
                var validation = await ValidateAsync(data, context);
                result.ValidationResult = validation;

                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Migration validation failed: {string.Join(", ", validation.Errors)}";
                    return result;
                }

                // Create backup if requested and not in dry-run mode
                if (context.CreateBackups && !context.DryRun)
                {
                    var backupPath = GenerateBackupPath(context.BackupDirectory);
                    var backupSuccess = await CreateBackupAsync(data, backupPath);

                    if (backupSuccess)
                    {
                        result.BackupPath = backupPath;
                        AuditLogger.Log("MIGRATION_BACKUP_CREATED", new { BackupPath = backupPath });
                    }
                    else
                    {
                        AuditLogger.LogWarning("MIGRATION_BACKUP_FAILED", new { BackupPath = backupPath });
                    }
                }

                // Execute migration
                var migratedData = context.DryRun
                    ? new List<T>(data)
                    : await ExecuteMigrationAsync(data, context);

                result.MigratedData = migratedData;
                result.Success = true;
                result.ItemsProcessed = migratedData.Count;

                // Validate after migration if requested
                if (context.ValidateAfterMigration && !context.DryRun)
                {
                    var postValidation = await ValidateAfterMigrationAsync(migratedData, context);
                    if (!postValidation.IsValid)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Post-migration validation failed: {string.Join(", ", postValidation.Errors)}";
                        return result;
                    }
                }

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;

                AuditLogger.Log("MIGRATION_COMPLETED", new
                {
                    MigrationName = Name,
                    Success = result.Success,
                    ItemsProcessed = result.ItemsProcessed,
                    Duration = result.Duration,
                    BackupPath = result.BackupPath
                }, category: AuditCategory.Data);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;

                AuditLogger.LogError("MIGRATION_FAILED", ex, new
                {
                    MigrationName = Name,
                    FromVersion = FromVersion.ToString(),
                    ToVersion = ToVersion.ToString()
                });

                return result;
            }
        }

        /// <inheritdoc />
        public virtual async Task<MigrationResult<T>> ReverseAsync(List<T> data, MigrationContext context)
        {
            if (!IsReversible)
            {
                throw new NotSupportedException($"Migration {Name} is not reversible");
            }

            var result = new MigrationResult<T>
            {
                FromVersion = ToVersion,
                ToVersion = FromVersion,
                StartTime = DateTime.Now,
                OriginalData = new List<T>(data)
            };

            try
            {
                AuditLogger.Log("MIGRATION_REVERSE_STARTED", new
                {
                    MigrationName = Name,
                    FromVersion = ToVersion.ToString(),
                    ToVersion = FromVersion.ToString(),
                    ItemCount = data.Count
                }, category: AuditCategory.Data);

                var reversedData = await ExecuteReverseMigrationAsync(data, context);

                result.MigratedData = reversedData;
                result.Success = true;
                result.ItemsProcessed = reversedData.Count;
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;

                AuditLogger.Log("MIGRATION_REVERSE_COMPLETED", new
                {
                    MigrationName = Name,
                    Success = result.Success,
                    ItemsProcessed = result.ItemsProcessed,
                    Duration = result.Duration
                }, category: AuditCategory.Data);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                result.EndTime = DateTime.Now;

                AuditLogger.LogError("MIGRATION_REVERSE_FAILED", ex, new { MigrationName = Name });
                return result;
            }
        }

        /// <inheritdoc />
        public virtual async Task<MigrationValidationResult> ValidateAsync(List<T> data, MigrationContext context)
        {
            var result = new MigrationValidationResult
            {
                IsValid = true,
                AffectedItemCount = data.Count,
                EstimatedDuration = EstimateMigrationDuration(data.Count)
            };

            // Basic validation
            if (data == null)
            {
                result.IsValid = false;
                result.Errors.Add("Data cannot be null");
                return result;
            }

            // Check if migration is applicable
            if (!CanMigrate(data))
            {
                result.IsValid = false;
                result.Errors.Add($"Migration {Name} cannot be applied to the current data");
                return result;
            }

            // Execute custom validation
            await ExecuteCustomValidationAsync(data, context, result);

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<bool> CreateBackupAsync(List<T> data, string backupPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath));

                var backupData = new MigrationBackup<T>
                {
                    EntityType = typeof(T).Name,
                    Version = FromVersion,
                    CreatedAt = DateTime.Now,
                    Data = data,
                    MigrationName = Name
                };

                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(backupPath, json);
                return true;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("MIGRATION_BACKUP_FAILED", ex, new { BackupPath = backupPath });
                return false;
            }
        }

        #region Abstract Methods

        /// <summary>
        /// Executes the actual migration logic
        /// </summary>
        /// <param name="data">Data to migrate</param>
        /// <param name="context">Migration context</param>
        /// <returns>Migrated data</returns>
        protected abstract Task<List<T>> ExecuteMigrationAsync(List<T> data, MigrationContext context);

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Executes reverse migration logic (override if IsReversible = true)
        /// </summary>
        /// <param name="data">Data to reverse</param>
        /// <param name="context">Migration context</param>
        /// <returns>Reversed data</returns>
        protected virtual Task<List<T>> ExecuteReverseMigrationAsync(List<T> data, MigrationContext context)
        {
            throw new NotSupportedException($"Reverse migration is not implemented for {Name}");
        }

        /// <summary>
        /// Executes custom validation logic
        /// </summary>
        /// <param name="data">Data to validate</param>
        /// <param name="context">Migration context</param>
        /// <param name="result">Validation result to populate</param>
        protected virtual Task ExecuteCustomValidationAsync(List<T> data, MigrationContext context, MigrationValidationResult result)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates data after migration
        /// </summary>
        /// <param name="data">Migrated data</param>
        /// <param name="context">Migration context</param>
        /// <returns>Validation result</returns>
        protected virtual Task<MigrationValidationResult> ValidateAfterMigrationAsync(List<T> data, MigrationContext context)
        {
            return Task.FromResult(new MigrationValidationResult { IsValid = true });
        }

        /// <summary>
        /// Checks if migration can be applied to the data
        /// </summary>
        /// <param name="data">Data to check</param>
        /// <returns>True if migration can be applied</returns>
        protected virtual bool CanMigrate(List<T> data)
        {
            return true;
        }

        /// <summary>
        /// Estimates migration duration based on data size
        /// </summary>
        /// <param name="itemCount">Number of items to migrate</param>
        /// <returns>Estimated duration</returns>
        protected virtual TimeSpan EstimateMigrationDuration(int itemCount)
        {
            // Default: 1ms per item + 1 second base overhead
            return TimeSpan.FromMilliseconds(itemCount + 1000);
        }

        /// <summary>
        /// Generates backup file path
        /// </summary>
        /// <param name="backupDirectory">Backup directory</param>
        /// <returns>Backup file path</returns>
        protected virtual string GenerateBackupPath(string backupDirectory)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"migration_backup_{typeof(T).Name}_{FromVersion}_{timestamp}.json";
            return Path.Combine(backupDirectory, fileName);
        }

        /// <summary>
        /// Reports migration progress
        /// </summary>
        /// <param name="context">Migration context</param>
        /// <param name="progress">Progress information</param>
        protected void ReportProgress(MigrationContext context, MigrationProgress progress)
        {
            context.ProgressCallback?.Invoke(progress);
        }

        #endregion
    }

    /// <summary>
    /// Migration backup data structure
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class MigrationBackup<T>
    {
        public string EntityType { get; set; }
        public DataVersion Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MigrationName { get; set; }
        public List<T> Data { get; set; }
    }
}