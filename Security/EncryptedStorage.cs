using SimpleDataEngine.Storage;
using SimpleDataEngine.Audit;
using System.Text.Json;
using System.Linq.Expressions;

namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Encrypted storage implementation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class EncryptedStorage<T> : IQueryableStorage<T>, IDisposable where T : class
    {
        private readonly EncryptionService _encryptionService;
        private readonly EncryptionConfig _config;
        private readonly string _filePath;
        private readonly object _lock = new object();
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        /// <summary>
        /// Initializes encrypted storage with configuration
        /// </summary>
        /// <param name="filePath">File path for storage</param>
        /// <param name="config">Encryption configuration</param>
        public EncryptedStorage(string filePath = null, EncryptionConfig config = null)
        {
            _config = config ?? new EncryptionConfig { EnableEncryption = true };
            _encryptionService = new EncryptionService(_config);

            if (string.IsNullOrEmpty(filePath))
            {
                string entityName = typeof(T).Name;
                string directoryf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                if (!Directory.Exists(directoryf))
                    Directory.CreateDirectory(directoryf);
                _filePath = Path.Combine(directoryf, $"{entityName}{_config.FileExtension}");
            }
            else
            {
                _filePath = ModifyFilePathForEncryption(filePath, _config);
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            AuditLogger.Log("ENCRYPTED_STORAGE_INITIALIZED", new
            {
                EntityType = typeof(T).Name,
                FilePath = _filePath,
                EncryptionEnabled = _config.EnableEncryption
            }, category: AuditCategory.Security);
        }

        /// <inheritdoc />
        public List<T> Load()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                try
                {
                    if (!File.Exists(_filePath))
                    {
                        AuditLogger.Log("ENCRYPTED_STORAGE_FILE_NOT_FOUND", new { FilePath = _filePath });
                        return new List<T>();
                    }

                    if (_config.EnableEncryption)
                    {
                        return LoadEncrypted();
                    }
                    else
                    {
                        return LoadUnencrypted();
                    }
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("ENCRYPTED_STORAGE_LOAD_FAILED", ex, new { FilePath = _filePath });
                    return new List<T>();
                }
            }
        }

        /// <inheritdoc />
        public void Save(List<T> items)
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                try
                {
                    if (_config.EnableEncryption)
                    {
                        SaveEncrypted(items);
                    }
                    else
                    {
                        SaveUnencrypted(items);
                    }

                    AuditLogger.Log("ENCRYPTED_STORAGE_SAVED", new
                    {
                        EntityType = typeof(T).Name,
                        ItemCount = items.Count,
                        FilePath = _filePath,
                        Encrypted = _config.EnableEncryption
                    }, category: AuditCategory.Security);
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("ENCRYPTED_STORAGE_SAVE_FAILED", ex, new
                    {
                        FilePath = _filePath,
                        ItemCount = items.Count
                    });
                    throw new InvalidOperationException($"Failed to save encrypted data to {_filePath}", ex);
                }
            }
        }

        /// <inheritdoc />
        public async Task<List<T>> LoadAsync()
        {
            return await Task.Run(() => Load());
        }

        /// <inheritdoc />
        public async Task SaveAsync(List<T> items)
        {
            await Task.Run(() => Save(items));
        }

        /// <inheritdoc />
        public List<T> Query(Expression<Func<T, bool>> predicate)
        {
            var items = Load();
            return items.Where(predicate.Compile()).ToList();
        }

        /// <inheritdoc />
        public async Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate)
        {
            var items = await LoadAsync();
            return items.Where(predicate.Compile()).ToList();
        }

        /// <inheritdoc />
        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            var items = Load();
            return predicate == null ? items.Count : items.Count(predicate.Compile());
        }

        /// <inheritdoc />
        public bool Any(Expression<Func<T, bool>> predicate = null)
        {
            var items = Load();
            return predicate == null ? items.Any() : items.Any(predicate.Compile());
        }

        /// <summary>
        /// Validates the integrity of encrypted storage
        /// </summary>
        /// <returns>True if storage is valid</returns>
        public bool ValidateIntegrity()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                try
                {
                    if (!File.Exists(_filePath))
                        return true; // Empty storage is valid

                    if (!_config.EnableEncryption)
                        return true; // Unencrypted storage validation

                    var encryptedData = File.ReadAllBytes(_filePath);
                    return _encryptionService.ValidateIntegrity(encryptedData);
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("ENCRYPTED_STORAGE_VALIDATION_FAILED", ex, new { FilePath = _filePath });
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets storage file information
        /// </summary>
        /// <returns>Storage file info</returns>
        public StorageInfo GetStorageInfo()
        {
            lock (_lock)
            {
                var fileInfo = new FileInfo(_filePath);

                return new StorageInfo
                {
                    FilePath = _filePath,
                    FileExists = fileInfo.Exists,
                    FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                    LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : null,
                    IsEncrypted = _config.EnableEncryption,
                    EncryptionType = _config.EncryptionType,
                    HasIntegrityCheck = _config.IncludeIntegrityCheck
                };
            }
        }

        #region Private Methods

        private List<T> LoadEncrypted()
        {
            var encryptedData = File.ReadAllBytes(_filePath);
            if (encryptedData.Length == 0)
                return new List<T>();

            var json = _encryptionService.Decrypt(encryptedData);
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }

        private List<T> LoadUnencrypted()
        {
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }

        private void SaveEncrypted(List<T> items)
        {
            var json = JsonSerializer.Serialize(items, _jsonOptions);
            var encryptedData = _encryptionService.Encrypt(json);
            File.WriteAllBytes(_filePath, encryptedData);
        }

        private void SaveUnencrypted(List<T> items)
        {
            var json = JsonSerializer.Serialize(items, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }

        private static string ModifyFilePathForEncryption(string filePath, EncryptionConfig config)
        {
            if (config?.EnableEncryption == true && !string.IsNullOrEmpty(filePath))
            {
                var extension = config.FileExtension ?? ".sde";
                return Path.ChangeExtension(filePath, extension);
            }
            return filePath;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedStorage<T>));
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _encryptionService?.Dispose();
                AuditLogger.Log("ENCRYPTED_STORAGE_DISPOSED", new { EntityType = typeof(T).Name });
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Storage information
    /// </summary>
    public class StorageInfo
    {
        public string FilePath { get; set; }
        public bool FileExists { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsEncrypted { get; set; }
        public EncryptionType EncryptionType { get; set; }
        public bool HasIntegrityCheck { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSizeBytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
        }
    }
}