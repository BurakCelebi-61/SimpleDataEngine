using System.Text.Json;

namespace SimpleDataEngine.Core
{
    /// <summary>
    /// Manages unique ID generation for entities
    /// </summary>
    public static class EntityIndexStorage
    {
        private static readonly string FilePath = "database/entity_index.json";
        private static Dictionary<string, int> _indexMap;
        private static readonly object _lock = new object();

        static EntityIndexStorage()
        {
            LoadIndexMap();
        }

        /// <summary>
        /// Gets the next available ID for an entity type
        /// </summary>
        /// <param name="entityName">Name of the entity type</param>
        /// <returns>Next available ID</returns>
        public static int GetNextId(string entityName)
        {
            lock (_lock)
            {
                if (_indexMap.ContainsKey(entityName))
                {
                    _indexMap[entityName]++;
                }
                else
                {
                    _indexMap[entityName] = 1;
                }

                Save();
                return _indexMap[entityName];
            }
        }

        /// <summary>
        /// Gets the current max ID for an entity type without incrementing
        /// </summary>
        /// <param name="entityName">Name of the entity type</param>
        /// <returns>Current max ID</returns>
        public static int GetCurrentId(string entityName)
        {
            lock (_lock)
            {
                return _indexMap.ContainsKey(entityName) ? _indexMap[entityName] : 0;
            }
        }

        /// <summary>
        /// Resets the ID counter for an entity type
        /// </summary>
        /// <param name="entityName">Name of the entity type</param>
        public static void ResetId(string entityName)
        {
            lock (_lock)
            {
                _indexMap[entityName] = 0;
                Save();
            }
        }

        private static void LoadIndexMap()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    _indexMap = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                }
                else
                {
                    _indexMap = new Dictionary<string, int>();
                    EnsureDirectoryExists();
                }
            }
            catch (Exception)
            {
                _indexMap = new Dictionary<string, int>();
                EnsureDirectoryExists();
            }
        }

        private static void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void Save()
        {
            try
            {
                EnsureDirectoryExists();
                var json = JsonSerializer.Serialize(_indexMap, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception)
            {
                // Silently ignore save errors to prevent application crashes
            }
        }
    }
}