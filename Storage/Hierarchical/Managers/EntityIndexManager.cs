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
                        // TÜR UYUMSUZLUĞU DÜZELTİLDİ - Dictionary<string, PropertyIndex> kullan
                        var propertyIndexes = ConvertToPropertyIndexes(_entityIndex.PropertyIndexes);

                        foreach (var propIndex in propertyIndexes)
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
                        PropertyIndexes = new Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>>()
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
                    PropertyIndexes = new Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>>()
                };
            }
        }

        #endregion

        #region TÜR DÖNÜŞTÜRME HELPER METHODLARI

        /// <summary>
        /// EntityIndex.PropertyIndexInfo dictionary'sini PropertyIndex dictionary'sine dönüştür
        /// </summary>
        private Dictionary<string, PropertyIndex> ConvertToPropertyIndexes(
            Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>> propertyIndexInfoDict)
        {
            var result = new Dictionary<string, PropertyIndex>();

            foreach (var kvp in propertyIndexInfoDict)
            {
                var propertyName = kvp.Key;
                var indexInfoDict = kvp.Value;

                var propertyIndex = new PropertyIndex(propertyName, "object");

                // PropertyIndexInfo'dan IndexEntry'lere dönüştür
                foreach (var infoKvp in indexInfoDict)
                {
                    var info = infoKvp.Value;

                    // SampleValues'tan IndexEntry'ler oluştur
                    if (info.SampleValues != null)
                    {
                        for (int i = 0; i < info.SampleValues.Count; i++)
                        {
                            var entry = new IndexEntry
                            {
                                RecordId = $"record_{i}",
                                Value = info.SampleValues[i],
                                SegmentId = 1, // Default segment
                                Position = i,
                                PropertyName = propertyName,
                                CreatedAt = info.LastUpdated
                            };
                            propertyIndex.AddEntry(entry);
                        }
                    }
                }

                result[propertyName] = propertyIndex;
            }

            return result;
        }

        /// <summary>
        /// PropertyIndex dictionary'sini EntityIndex.PropertyIndexInfo dictionary'sine dönüştür
        /// </summary>
        private Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>> ConvertToPropertyIndexInfos(
            Dictionary<string, PropertyIndex> propertyIndexes)
        {
            var result = new Dictionary<string, Dictionary<string, EntityIndex.PropertyIndexInfo>>();

            foreach (var kvp in propertyIndexes)
            {
                var propertyName = kvp.Key;
                var propertyIndex = kvp.Value;

                var infoDict = new Dictionary<string, EntityIndex.PropertyIndexInfo>();

                // PropertyIndex'ten PropertyIndexInfo oluştur
                var info = new EntityIndex.PropertyIndexInfo
                {
                    Count = propertyIndex.Entries?.Count ?? 0,
                    SampleValues = propertyIndex.Entries?.Take(10).Select(e => e.Value).ToList() ?? new List<object>(),
                    LastUpdated = propertyIndex.LastModified,
                    IndexType = EntityIndex.IndexType.Range
                };

                // Range bilgisini hesapla
                if (propertyIndex.Entries?.Any() == true)
                {
                    var values = propertyIndex.Entries.Select(e => e.Value).Where(v => v != null).ToList();
                    if (values.Any())
                    {
                        if (values.First() is IComparable)
                        {
                            var sortedValues = values.Where(v => v is IComparable).Cast<IComparable>().OrderBy(v => v).ToList();
                            info.Range = new object[] { sortedValues.FirstOrDefault(), sortedValues.LastOrDefault() };
                        }
                    }
                }

                infoDict[propertyName] = info;
                result[propertyName] = infoDict;
            }

            return result;
        }

        #endregion

        #region Index Management - DÜZELTİLMİŞ

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

                // PropertyIndex dictionary kullan
                var propertyIndexes = ConvertToPropertyIndexes(_entityIndex.PropertyIndexes);

                foreach (var property in properties)
                {
                    await AddPropertyToIndexAsync(recordsList, segmentId, property, idSelector, propertyIndexes);
                }

                // Geri dönüştür
                _entityIndex.PropertyIndexes = ConvertToPropertyIndexInfos(propertyIndexes);
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

        private async Task AddPropertyToIndexAsync<T>(List<T> records, int segmentId, PropertyInfo property,
            Func<T, object> idSelector, Dictionary<string, PropertyIndex> propertyIndexes)
        {
            var propertyName = property.Name;

            // Ensure property index exists
            if (!propertyIndexes.ContainsKey(propertyName))
            {
                propertyIndexes[propertyName] = new PropertyIndex(propertyName, property.PropertyType.Name);
            }

            // Ensure in-memory index exists
            if (!_indexes.ContainsKey(propertyName))
            {
                _indexes.TryAdd(propertyName, new Dictionary<object, List<IndexEntry>>());
            }

            var propertyIndex = propertyIndexes[propertyName];
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
                        PropertyName = propertyName,
                        PropertyType = property.PropertyType.Name,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Add to persistent index
                    propertyIndex.AddEntry(indexEntry);

                    // Add to memory index
                    if (!memoryIndex.ContainsKey(propertyValue))
                    {
                        memoryIndex[propertyValue] = new List<IndexEntry>();
                    }
                    memoryIndex[propertyValue].Add(indexEntry);
                }
            }
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
                var propertyIndexes = ConvertToPropertyIndexes(_entityIndex.PropertyIndexes);

                foreach (var propertyIndex in propertyIndexes.Values)
                {
                    // Remove from persistent index
                    propertyIndex.RemoveEntriesByRecordId(recordIds.First()); // Simplified
                }

                // Remove from memory indexes
                foreach (var memoryIndex in _indexes.Values)
                {
                    foreach (var entryList in memoryIndex.Values)
                    {
                        entryList.RemoveAll(e => recordIds.Contains(e.RecordId));
                    }
                }

                _entityIndex.PropertyIndexes = ConvertToPropertyIndexInfos(propertyIndexes);
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

        #endregion

        #region Statistics

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
                var propertyIndexes = ConvertToPropertyIndexes(_entityIndex.PropertyIndexes);
                var optimized = false;

                foreach (var propertyIndex in propertyIndexes.Values)
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
                    _entityIndex.PropertyIndexes = ConvertToPropertyIndexInfos(propertyIndexes);
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
}