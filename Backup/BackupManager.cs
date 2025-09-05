using SimpleDataEngine.Audit;
using SimpleDataEngine.Configuration;
using System.IO.Compression;

namespace SimpleDataEngine.Backup
{

    /// <summary>
    /// Manages backup operations for SimpleDataEngine
    /// </summary>
    public static class BackupManager
    {
        private static readonly string BackupDirectory;
        private static readonly object _lock = new object();

        static BackupManager()
        {
            BackupDirectory = ConfigManager.Current.Database.BackupDirectory;
            EnsureBackupDirectoryExists();
        }

        /// <summary>
        /// Creates a backup of all data files
        /// </summary>
        /// <param name="description">Optional backup description</param>
        /// <returns>Path to the created backup</returns>
        public static string CreateBackup(string description = null)
        {
            lock (_lock)
            {
                try
                {
                    var config = ConfigManager.Current.Backup;
                    var timestamp = DateTime.Now;
                    var backupName = string.Format(config.BackupNameFormat, timestamp);

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        backupName = $"{backupName}_{SanitizeFileName(description)}";
                    }

                    var extension = config.CompressBackups ? ".zip" : ".bak";
                    var backupPath = Path.Combine(BackupDirectory, backupName + extension);

                    // Create backup
                    if (config.CompressBackups)
                    {
                        CreateCompressedBackup(backupPath);
                    }
                    else
                    {
                        CreateFolderBackup(backupPath);
                    }

                    // Log backup creation
                    AuditLogger.Log("BACKUP_CREATED", new
                    {
                        BackupPath = backupPath,
                        Description = description,
                        Compressed = config.CompressBackups,
                        Timestamp = timestamp
                    });

                    // Clean old backups if needed
                    if (config.MaxBackups > 0)
                    {
                        CleanOldBackups(config.MaxBackups);
                    }

                    return backupPath;
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("BACKUP_FAILED", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates an async backup
        /// </summary>
        /// <param name="description">Optional backup description</param>
        /// <returns>Task with backup path</returns>
        public static async Task<string> CreateBackupAsync(string description = null)
        {
            return await Task.Run(() => CreateBackup(description));
        }

        /// <summary>
        /// Restores data from a backup
        /// </summary>
        /// <param name="backupPath">Path to backup file</param>
        /// <param name="validateBeforeRestore">Whether to validate backup before restoring</param>
        public static void RestoreBackup(string backupPath, bool validateBeforeRestore = true)
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(backupPath))
                    {
                        throw new FileNotFoundException($"Backup file not found: {backupPath}");
                    }

                    // Validate backup if requested
                    if (validateBeforeRestore)
                    {
                        var validation = ValidateBackup(backupPath);
                        if (!validation.IsValid)
                        {
                            throw new InvalidOperationException(
                                $"Backup validation failed: {string.Join(", ", validation.Errors)}");
                        }
                    }

                    // Create current backup before restore
                    var currentBackup = CreateBackup("BeforeRestore");

                    try
                    {
                        var dataDirectory = ConfigManager.Current.Database.DataDirectory;

                        // Clear current data
                        if (Directory.Exists(dataDirectory))
                        {
                            Directory.Delete(dataDirectory, true);
                        }
                        Directory.CreateDirectory(dataDirectory);

                        // Extract backup
                        if (Path.GetExtension(backupPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            ZipFile.ExtractToDirectory(backupPath, dataDirectory);
                        }
                        else
                        {
                            // Folder backup - copy files
                            CopyDirectory(backupPath, dataDirectory);
                        }

                        AuditLogger.Log("BACKUP_RESTORED", new
                        {
                            BackupPath = backupPath,
                            RestoreTime = DateTime.Now,
                            SafetyBackup = currentBackup
                        });
                    }
                    catch
                    {
                        // If restore fails, try to restore the safety backup
                        try
                        {
                            RestoreBackup(currentBackup, false);
                            AuditLogger.Log("SAFETY_BACKUP_RESTORED", new { SafetyBackup = currentBackup });
                        }
                        catch (Exception restoreEx)
                        {
                            AuditLogger.LogError("SAFETY_BACKUP_RESTORE_FAILED", restoreEx);
                        }
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("BACKUP_RESTORE_FAILED", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets list of available backups
        /// </summary>
        /// <returns>List of backup information</returns>
        public static List<BackupInfo> GetAvailableBackups()
        {
            var backups = new List<BackupInfo>();

            if (!Directory.Exists(BackupDirectory))
                return backups;

            try
            {
                var files = Directory.GetFiles(BackupDirectory, "*.*")
                    .Where(f => Path.GetExtension(f).ToLower() is ".zip" or ".bak");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    // Parse description from filename if exists
                    string description = null;
                    if (fileName.Contains("_"))
                    {
                        var parts = fileName.Split('_');
                        if (parts.Length > 1)
                        {
                            description = string.Join("_", parts.Skip(1));
                        }
                    }

                    backups.Add(new BackupInfo
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        CreatedAt = fileInfo.CreationTime,
                        FileSizeBytes = fileInfo.Length,
                        Description = description,
                        IsCompressed = Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase),
                        Version = "1.0" // Could be read from backup metadata
                    });
                }

                return backups.OrderByDescending(b => b.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("GET_BACKUPS_FAILED", ex);
                return backups;
            }
        }

        /// <summary>
        /// Validates a backup file
        /// </summary>
        /// <param name="backupPath">Path to backup file</param>
        /// <returns>Validation result</returns>
        public static BackupValidationResult ValidateBackup(string backupPath)
        {
            var result = new BackupValidationResult { IsValid = true };

            try
            {
                if (!File.Exists(backupPath))
                {
                    result.IsValid = false;
                    result.Errors.Add("Backup file does not exist");
                    return result;
                }

                var fileInfo = new FileInfo(backupPath);
                if (fileInfo.Length == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Backup file is empty");
                    return result;
                }

                // Validate compressed backup
                if (Path.GetExtension(backupPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var archive = ZipFile.OpenRead(backupPath);

                        if (archive.Entries.Count == 0)
                        {
                            result.IsValid = false;
                            result.Errors.Add("Backup archive is empty");
                        }

                        // Check for required files
                        var requiredExtensions = new[] { ".json", ".db" };
                        var hasRequiredFiles = archive.Entries.Any(e =>
                            requiredExtensions.Any(ext => e.FullName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

                        if (!hasRequiredFiles)
                        {
                            result.Warnings.Add("Backup may not contain data files");
                        }

                        result.Metadata["EntryCount"] = archive.Entries.Count;
                        result.Metadata["CompressedSize"] = fileInfo.Length;
                        result.Metadata["UncompressedSize"] = archive.Entries.Sum(e => e.Length);
                    }
                    catch (InvalidDataException)
                    {
                        result.IsValid = false;
                        result.Errors.Add("Backup archive is corrupted");
                    }
                }

                result.Metadata["FileSize"] = fileInfo.Length;
                result.Metadata["CreatedAt"] = fileInfo.CreationTime;
                result.Metadata["LastModified"] = fileInfo.LastWriteTime;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Deletes a backup file
        /// </summary>
        /// <param name="backupPath">Path to backup file</param>
        public static void DeleteBackup(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    AuditLogger.Log("BACKUP_DELETED", new { BackupPath = backupPath });
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("BACKUP_DELETE_FAILED", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets backup directory size
        /// </summary>
        /// <returns>Total size in bytes</returns>
        public static long GetBackupDirectorySize()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                    return 0;

                return Directory.GetFiles(BackupDirectory, "*.*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch
            {
                return 0;
            }
        }

        #region Private Methods

        private static void CreateCompressedBackup(string backupPath)
        {
            var dataDirectory = ConfigManager.Current.Database.DataDirectory;

            if (!Directory.Exists(dataDirectory))
            {
                throw new DirectoryNotFoundException($"Data directory not found: {dataDirectory}");
            }

            using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);

            var files = Directory.GetFiles(dataDirectory, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(dataDirectory, file);
                archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
            }
        }

        private static void CreateFolderBackup(string backupPath)
        {
            var dataDirectory = ConfigManager.Current.Database.DataDirectory;

            if (!Directory.Exists(dataDirectory))
            {
                throw new DirectoryNotFoundException($"Data directory not found: {dataDirectory}");
            }

            CopyDirectory(dataDirectory, backupPath);
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private static void CleanOldBackups(int maxBackups)
        {
            try
            {
                var backups = GetAvailableBackups();
                if (backups.Count <= maxBackups)
                    return;

                var backupsToDelete = backups
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip(maxBackups);

                foreach (var backup in backupsToDelete)
                {
                    DeleteBackup(backup.FilePath);
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("BACKUP_CLEANUP_FAILED", ex);
            }
        }

        private static void EnsureBackupDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                {
                    Directory.CreateDirectory(BackupDirectory);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot create backup directory: {BackupDirectory}", ex);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        #endregion
    }
}