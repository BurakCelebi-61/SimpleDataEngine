using SimpleDataEngine.Audit;
using SimpleDataEngine.Storage.Hierarchical.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Manages indexes for a specific entity
    /// </summary>
    public class EntityIndexManager : IDisposable
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly string _entityName;
        private readonly string _indexFilePath;
        private readonly ConcurrentDictionary<string, Dictionary<object, List<IndexEntry>>> _indexes;
        private readonly SemaphoreSlim _indexLock;
        private EntityIndex _entityIndex;
        private bool _disposed;

        public EntityIndexManager(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _indexFilePath = Path.Combine(config.DataModelsPath, entityName, $"index{config.FileExtension}");
            _indexes = new ConcurrentDictionary<string, Dictionary<object, List<IndexEntry>>>();
            _indexLock = new SemaphoreSlim(1, 1);
        }

        #region Initialization

        /// <summary>
        /// Initialize index manager
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadIndexAsync();
        }

        private async Task LoadIndexAsync()
        {
            try
            {
                if (File.Exists(_indexFilePath))
                {
                    _entityIndex = await _fileHandler.ReadAsync<EntityIndex>(_indexFilePath);

                    // Load indexes into memory for fast access
                    if (_entityIndex?.PropertyIndexes != null)
                    {
                        foreach (var propIndex in _entityIndex.PropertyIndexes)
                        {
                            var indexDict = new Dictionary<object, List<IndexEntry>>();

                            foreach (var entry in propIndex.Value.Entries)
                            {
                                if (!indexDict.ContainsKey(entry.Value))
                                {
                                    indexDict[entry.Value] = new List<IndexEntry>();
                                }
                                indexDict[entry.Value].Add(entry);
                            }

                            _indexes.TryAdd(propIndex.Key, indexDict);
                        }
                    }
                }
                else
                {
                    _entityIndex = new EntityIndex
                    {
                        EntityName = _entityName,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        PropertyIndexes = new Dictionary<string, PropertyIndex>()
                    };
                }
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to load index for entity {_entityName}", ex);

                // Create new index on failure
                _entityIndex = new EntityIndex
                {
                    EntityName = _entityName,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    PropertyIndexes = new Dictionary<string, PropertyIndex>()
                };
            }
        }

        #endregion

        #region Index Management

        /// <summary>
        /// Add records to index
        /// </summary>
        public async Task AddToIndexAsync<T>(IEnumerable<T> records, int segmentId, Func<T, object> idSelector) where T : class
        {
            if (!records.Any()) return;

            await _indexLock.WaitAsync();
            try
            {
                var recordsList = records.ToList();
                var properties = GetIndexableProperties<T>();

                foreach (var property in properties)
                {
                    await AddPropertyToIndexAsync(recordsList, segmentId, property, idSelector);
                }

                _entityIndex.LastModified = DateTime.UtcNow;
                _entityIndex.TotalRecords = (_entityIndex.TotalRecords ?? 0) + recordsList.Count;

                await SaveIndexAsync();

                await AuditLogger.LogAsync($"Records added to index for entity {_entityName}",
                    new { RecordCount = recordsList.Count, SegmentId = segmentId });
            }
            finally
            {
                _indexLock.Release();
            }
        }

        private async Task AddPropertyToIndexAsync<T>(List<T> records, int segmentId, PropertyInfo property, Func<T, object> idSelector)
        {
            var propertyName = property.Name;

            // Ensure property index exists
            if (!_entityIndex.PropertyIndexes.ContainsKey(propertyName))
            {
                _entityIndex.PropertyIndexes[propertyName] = new PropertyIndex
                {
                    PropertyName = propertyName,
                    PropertyType = property.PropertyType.Name,
                    CreatedAt = DateTime.UtcNow,
                    Entries = new List<IndexEntry>()
                };
            }

            // Ensure in-memory index exists
            if (!_indexes.ContainsKey(propertyName))
            {
                _indexes.TryAdd(propertyName, new Dictionary<object, List<IndexEntry>>());
            }

            var propertyIndex = _entityIndex.PropertyIndexes[propertyName];
            var memoryIndex = _indexes[propertyName];

            // Add entries
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var recordId = idSelector(record);
                var propertyValue = property.GetValue(record);

                if (propertyValue != null)
                {
                    var indexEntry = new IndexEntry
                    {
                        RecordId = recordId,
                        Value = propertyValue,
                        SegmentId = segmentId,
                        Position = i,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Add to persistent index
                    propertyIndex.Entries.Add(indexEntry);

                    // Add to memory index
                    if (!memoryIndex.ContainsKey(propertyValue))
                    {
                        memoryIndex[propertyValue] = new List<IndexEntry>();
                    }
                    memoryIndex[propertyValue].Add(indexEntry);
                }
            }

            propertyIndex.LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Remove records from index
        /// </summary>
        public async Task RemoveFromIndexAsync<T>(IEnumerable<T> records, Func<T, object> idSelector) where T : class
        {
            if (!records.Any()) return;

            await _indexLock.WaitAsync();
            try
            {
                var recordIds = records.Select(idSelector).ToHashSet();

                foreach (var propertyIndex in _entityIndex.PropertyIndexes.Values)
                {
                    // Remove from persistent index
                    propertyIndex.Entries.RemoveAll(e => recordIds.Contains(e.RecordId));
                    propertyIndex.LastModified = DateTime.UtcNow;
                }

                // Remove from memory indexes
                foreach (var memoryIndex in _indexes.Values)
                {
                    foreach (var entryList in memoryIndex.Values)
                    {
                        entryList.RemoveAll(e => recordIds.Contains(e.RecordId));
                    }
                }

                _entityIndex.LastModified = DateTime.UtcNow;
                _entityIndex.TotalRecords = Math.Max(0, (_entityIndex.TotalRecords ?? 0) - records.Count());

                await SaveIndexAsync();

                await AuditLogger.LogAsync($"Records removed from index for entity {_entityName}",
                    new { RecordCount = records.Count() });
            }
            finally
            {
                _indexLock.Release();
            }
        }

        /// <summary>
        /// Update records in index
        /// </summary>
        public async Task UpdateIndexAsync<T>(IEnumerable<T> oldRecords, IEnumerable<T> newRecords,
            int segmentId, Func<T, object> idSelector) where T : class
        {
            await RemoveFromIndexAsync(oldRecords, idSelector);
            await AddToIndexAsync(newRecords, segmentId, idSelector);
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Find records by property value
        /// </summary>
        public async Task<List<IndexEntry>> FindByPropertyAsync(string propertyName, object value)
        {
            if (!_indexes.TryGetValue(propertyName, out var propertyIndex))
            {
                return new List<IndexEntry>();
            }

            if (!propertyIndex.TryGetValue(value, out var entries))
            {
                return new List<IndexEntry>();
            }

            return new List<IndexEntry>(entries);
        }

        /// <summary>
        /// Find records by multiple property values (AND operation)
        /// </summary>
        public async Task<List<IndexEntry>> FindByPropertiesAsync(Dictionary<string, object> propertyValues)
        {
            if (!propertyValues.Any()) return new List<IndexEntry>();

            List<IndexEntry> result = null;

            foreach (var kvp in propertyValues)
            {
                var entries = await FindByPropertyAsync(kvp.Key, kvp.Value);

                if (result == null)
                {
                    result = entries;
                }
                else
                {
                    // Intersect with previous results (AND operation)
                    result = result.Intersect(entries, new IndexEntryComparer()).ToList();
                }

                // Early exit if no matches
                if (!result.Any()) break;
            }

            return result ?? new List<IndexEntry>();
        }

        /// <summary>
        /// Find records by property value range
        /// </summary>
        public async Task<List<IndexEntry>> FindByRangeAsync<TValue>(string propertyName, TValue minValue, TValue maxValue)
            where TValue : IComparable<TValue>
        {
            if (!_indexes.TryGetValue(propertyName, out var propertyIndex))
            {
                return new List<IndexEntry>();
            }

            var result = new List<IndexEntry>();

            foreach (var kvp in propertyIndex)
            {
                if (kvp.Key is TValue value &&
                    value.CompareTo(minValue) >= 0 &&
                    value.CompareTo(maxValue) <= 0)
                {
                    result.AddRange(kvp.Value);
                }
            }

            return result.OrderBy(e => e.CreatedAt).ToList();
        }

        /// <summary>
        /// Get all distinct values for a property
        /// </summary>
        public async Task<List<object>> GetDistinctValuesAsync(string propertyName)
        {
            if (!_indexes.TryGetValue(propertyName, out var propertyIndex))
            {
                return new List<object>();
            }

            return propertyIndex.Keys.ToList();
        }

        /// <summary>
        /// Get record count by property value
        /// </summary>
        public async Task<Dictionary<object, int>> GetCountByPropertyAsync(string propertyName)
        {
            if (!_indexes.TryGetValue(propertyName, out var propertyIndex))
            {
                return new Dictionary<object, int>();
            }

            return propertyIndex.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            );
        }

        /// <summary>
        /// Get segments containing records with specific property value
        /// </summary>
        public async Task<List<int>> GetSegmentsByPropertyAsync(string propertyName, object value)
        {
            var entries = await FindByPropertyAsync(propertyName, value);
            return entries.Select(e => e.SegmentId).Distinct().OrderBy(id => id).ToList();
        }

        #endregion

        #region Index Statistics

        /// <summary>
        /// Get index statistics
        /// </summary>
        public async Task<IndexStatistics> GetStatisticsAsync()
        {
            var stats = new IndexStatistics
            {
                EntityName = _entityName,
                TotalRecords = _entityIndex.TotalRecords ?? 0,
                IndexedProperties = _entityIndex.PropertyIndexes?.Count ?? 0,
                CreatedAt = _entityIndex.CreatedAt,
                LastModified = _entityIndex.LastModified
            };

            if (_entityIndex.PropertyIndexes != null)
            {
                stats.PropertyStatistics = _entityIndex.PropertyIndexes.Select(kvp => new PropertyStatistics
                {
                    PropertyName = kvp.Key,
                    PropertyType = kvp.Value.PropertyType,
                    UniqueValues = _indexes.ContainsKey(kvp.Key) ? _indexes[kvp.Key].Count : 0,
                    TotalEntries = kvp.Value.Entries?.Count ?? 0,
                    LastModified = kvp.Value.LastModified
                }).ToList();
            }

            return stats;
        }

        #endregion

        #region Maintenance Operations

        /// <summary>
        /// Rebuild all indexes
        /// </summary>
        public async Task RebuildIndexesAsync()
        {
            await _indexLock.WaitAsync();
            try
            {
                // Clear existing indexes
                _indexes.Clear();
                _entityIndex.PropertyIndexes.Clear();
                _entityIndex.LastModified = DateTime.UtcNow;
                _entityIndex.TotalRecords = 0;

                await SaveIndexAsync();

                await AuditLogger.LogAsync($"Indexes rebuilt for entity {_entityName}");
            }
            finally
            {
                _indexLock.Release();
            }
        }

        /// <summary>
        /// Optimize indexes by removing orphaned entries
        /// </summary>
        public async Task OptimizeIndexesAsync()
        {
            await _indexLock.WaitAsync();
            try
            {
                var optimized = false;

                foreach (var propertyIndex in _entityIndex.PropertyIndexes.Values)
                {
                    var beforeCount = propertyIndex.Entries.Count;

                    // Remove duplicate entries
                    var uniqueEntries = propertyIndex.Entries
                        .GroupBy(e => new { e.RecordId, e.Value })
                        .Select(g => g.OrderByDescending(e => e.CreatedAt).First())
                        .ToList();

                    if (uniqueEntries.Count != beforeCount)
                    {
                        propertyIndex.Entries = uniqueEntries;
                        propertyIndex.LastModified = DateTime.UtcNow;
                        optimized = true;
                    }
                }

                if (optimized)
                {
                    _entityIndex.LastModified = DateTime.UtcNow;
                    await SaveIndexAsync();

                    // Reload memory indexes
                    await LoadIndexAsync();

                    await AuditLogger.LogAsync($"Indexes optimized for entity {_entityName}");
                }
            }
            finally
            {
                _indexLock.Release();
            }
        }

        /// <summary>
        /// Remove index entries for a specific segment
        /// </summary>
        public async Task RemoveSegmentFromIndexAsync(int segmentId)
        {
            await _indexLock.WaitAsync();
            try
            {
                var removedCount = 0;

                foreach (var propertyIndex in _entityIndex.PropertyIndexes.Values)
                {
                    var beforeCount = propertyIndex.Entries.Count;
                    propertyIndex.Entries.RemoveAll(e => e.SegmentId == segmentId);
                    removedCount += beforeCount - propertyIndex.Entries.Count;

                    if (beforeCount != propertyIndex.Entries.Count)
                    {
                        propertyIndex.LastModified = DateTime.UtcNow;
                    }
                }

                // Remove from memory indexes
                foreach (var memoryIndex in _indexes.Values)
                {
                    foreach (var entryList in memoryIndex.Values)
                    {
                        entryList.RemoveAll(e => e.SegmentId == segmentId);
                    }
                }

                if (removedCount > 0)
                {
                    _entityIndex.LastModified = DateTime.UtcNow;
                    _entityIndex.TotalRecords = Math.Max(0, (_entityIndex.TotalRecords ?? 0) - removedCount);

                    await SaveIndexAsync();

                    await AuditLogger.LogAsync($"Segment {segmentId} removed from indexes for entity {_entityName}",
                        new { RemovedEntries = removedCount });
                }
            }
            finally
            {
                _indexLock.Release();
            }
        }

        #endregion

        #region Utilities

        private async Task SaveIndexAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_indexFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await _fileHandler.WriteAsync(_indexFilePath, _entityIndex);
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to save index for entity {_entityName}", ex);
                throw;
            }
        }

        private PropertyInfo[] GetIndexableProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsIndexableType(p.PropertyType))
                .ToArray();
        }

        private bool IsIndexableType(Type type)
        {
            // Support basic types for indexing
            var nullableType = Nullable.GetUnderlyingType(type) ?? type;

            return nullableType.IsPrimitive ||
                   nullableType == typeof(string) ||
                   nullableType == typeof(DateTime) ||
                   nullableType == typeof(DateTimeOffset) ||
                   nullableType == typeof(TimeSpan) ||
                   nullableType == typeof(Guid) ||
                   nullableType == typeof(decimal);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _indexLock?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Index entry comparer for intersection operations
    /// </summary>
        #endregion
}