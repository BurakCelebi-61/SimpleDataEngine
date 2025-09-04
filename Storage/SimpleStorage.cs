using SimpleDataEngine.Configuration;
using System.Linq.Expressions;
using System.Text.Json;

namespace SimpleDataEngine.Storage
{
    /// <summary>
    /// Simple file-based storage implementation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class SimpleStorage<T> : IQueryableStorage<T> where T : class
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of SimpleStorage
        /// </summary>
        public SimpleStorage()
        {
            string entityName = typeof(T).Name;
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                ConfigManager.Current.Database.ConnectionString);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _filePath = Path.Combine(directory, $"{entityName}.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Initializes with custom file path
        /// </summary>
        /// <param name="filePath">Custom file path</param>
        public SimpleStorage(string filePath)
        {
            _filePath = filePath;

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <inheritdoc />
        public List<T> Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                        return new List<T>();

                    var json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                        return new List<T>();

                    return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
                }
                catch (Exception)
                {
                    // Return empty list on deserialization errors
                    return new List<T>();
                }
            }
        }

        /// <inheritdoc />
        public void Save(List<T> items)
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(items, _jsonOptions);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to save data to {_filePath}", ex);
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
        /// Gets the file path used by this storage
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Gets the size of the storage file in bytes
        /// </summary>
        public long FileSize
        {
            get
            {
                try
                {
                    return File.Exists(_filePath) ? new FileInfo(_filePath).Length : 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the last modified time of the storage file
        /// </summary>
        public DateTime? LastModified
        {
            get
            {
                try
                {
                    return File.Exists(_filePath) ? File.GetLastWriteTime(_filePath) : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Checks if the storage file exists
        /// </summary>
        public bool FileExists => File.Exists(_filePath);

        /// <summary>
        /// Deletes the storage file
        /// </summary>
        public void DeleteFile()
        {
            lock (_lock)
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
        }

        /// <summary>
        /// Creates a copy of the storage file
        /// </summary>
        /// <param name="destinationPath">Destination path for the copy</param>
        public void CopyTo(string destinationPath)
        {
            lock (_lock)
            {
                if (File.Exists(_filePath))
                {
                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                        Directory.CreateDirectory(destinationDir);

                    File.Copy(_filePath, destinationPath, overwrite: true);
                }
            }
        }
    }
}