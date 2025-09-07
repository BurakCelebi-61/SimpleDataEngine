using SimpleDataEngine.Audit;
using SimpleDataEngine.Storage.Hierarchical.Managers;
using System.Linq.Expressions;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Entity-specific hierarchical storage implementation
    /// </summary>
    public class HierarchicalStorage<T> : IDisposable where T : class
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly string _entityName;
        private readonly SegmentManager _segmentManager;
        private readonly EntityIndexManager _indexManager;
        private readonly EntityMetadataManager _metadataManager;
        private readonly SemaphoreSlim _operationLock;
        private bool _disposed;

        // Default ID selector (looks for Id, ID, or first property)
        private readonly Func<T, object> _defaultIdSelector;

        public HierarchicalStorage(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));

            _segmentManager = new SegmentManager(config, fileHandler, entityName);
            _indexManager = new EntityIndexManager(config, fileHandler, entityName);
            _metadataManager = new EntityMetadataManager(config, fileHandler, entityName);
            _operationLock = new SemaphoreSlim(1, 1);

            _defaultIdSelector = CreateDefaultIdSelector();

            // Initialize managers
            Task.Run(async () =>
            {
                await _segmentManager.InitializeAsync();
                await _indexManager.InitializeAsync();
                await _metadataManager.InitializeAsync();
            });
        }

        #region CRUD Operations

        /// <summary>
        /// Insert records into storage
        /// </summary>
        public async Task InsertAsync(IEnumerable<T> records, Func<T, object> idSelector = null)
        {
            ThrowIfDisposed();
            if (!records.Any()) return;

            idSelector ??= _defaultIdSelector;
            var recordsList = records.ToList();

            await _operationLock.WaitAsync();
            try
            {
                // Get active segment
                var segmentId = await _segmentManager.GetActiveSegmentIdAsync();

                // Read existing data from segment
                var existingData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);

                // Add new records
                existingData.Records.AddRange(recordsList);
                existingData.LastModified = DateTime.UtcNow;

                // Write back to segment
                await _segmentManager.WriteToSegmentAsync(segmentId, existingData.Records);

                // Update indexes if enabled
                if (_config.EnableRealTimeIndexing)
                {
                    await _indexManager.AddToIndexAsync(recordsList, segmentId, idSelector);
                }

                await AuditLogger.LogAsync($"Records inserted into {_entityName}",
                    new { RecordCount = recordsList.Count, SegmentId = segmentId });
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Insert single record
        /// </summary>
        public async Task InsertAsync(T record, Func<T, object> idSelector = null)
        {
            await InsertAsync(new[] { record }, idSelector);
        }

        /// <summary>
        /// Update records in storage
        /// </summary>
        public async Task UpdateAsync(IEnumerable<T> records, Func<T, object> idSelector = null)
        {
            ThrowIfDisposed();
            if (!records.Any()) return;

            idSelector ??= _defaultIdSelector;
            var recordsList = records.ToList();
            var recordIds = recordsList.Select(idSelector).ToHashSet();

            await _operationLock.WaitAsync();
            try
            {
                var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();
                var updatedSegments = new List<int>();

                foreach (var segmentId in segmentIds)
                {
                    var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                    var originalRecords = segmentData.Records.ToList();
                    var modified = false;

                    // Replace matching records
                    for (int i = 0; i < segmentData.Records.Count; i++)
                    {
                        var recordId = idSelector(segmentData.Records[i]);
                        if (recordIds.Contains(recordId))
                        {
                            var newRecord = recordsList.First(r => Equals(idSelector(r), recordId));
                            segmentData.Records[i] = newRecord;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        segmentData.LastModified = DateTime.UtcNow;
                        await _segmentManager.WriteToSegmentAsync(segmentId, segmentData.Records);
                        updatedSegments.Add(segmentId);

                        // Update indexes if enabled
                        if (_config.EnableRealTimeIndexing)
                        {
                            var oldRecords = originalRecords.Where(r => recordIds.Contains(idSelector(r)));
                            var newRecords = recordsList.Where(r => recordIds.Contains(idSelector(r)));
                            await _indexManager.UpdateIndexAsync(oldRecords, newRecords, segmentId, idSelector);
                        }
                    }
                }

                await AuditLogger.LogAsync($"Records updated in {_entityName}",
                    new { RecordCount = recordsList.Count, UpdatedSegments = updatedSegments.Count });
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Update single record
        /// </summary>
        public async Task UpdateAsync(T record, Func<T, object> idSelector = null)
        {
            await UpdateAsync(new[] { record }, idSelector);
        }

        /// <summary>
        /// Delete records from storage
        /// </summary>
        public async Task DeleteAsync(IEnumerable<object> recordIds, Func<T, object> idSelector = null)
        {
            ThrowIfDisposed();
            if (!recordIds.Any()) return;

            idSelector ??= _defaultIdSelector;
            var idsToDelete = recordIds.ToHashSet();

            await _operationLock.WaitAsync();
            try
            {
                var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();
                var deletedRecords = new List<T>();

                foreach (var segmentId in segmentIds)
                {
                    var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                    var originalCount = segmentData.Records.Count;

                    // Collect records to be deleted for index removal
                    var recordsToDelete = segmentData.Records.Where(r => idsToDelete.Contains(idSelector(r))).ToList();
                    deletedRecords.AddRange(recordsToDelete);

                    // Remove records
                    segmentData.Records.RemoveAll(r => idsToDelete.Contains(idSelector(r)));

                    if (segmentData.Records.Count != originalCount)
                    {
                        segmentData.LastModified = DateTime.UtcNow;
                        await _segmentManager.WriteToSegmentAsync(segmentId, segmentData.Records);
                    }
                }

                // Update indexes if enabled
                if (_config.EnableRealTimeIndexing && deletedRecords.Any())
                {
                    await _indexManager.RemoveFromIndexAsync(deletedRecords, idSelector);
                }

                await AuditLogger.LogAsync($"Records deleted from {_entityName}",
                    new { DeletedCount = deletedRecords.Count, RequestedIds = recordIds.Count() });
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Delete single record by ID
        /// </summary>
        public async Task DeleteAsync(object recordId, Func<T, object> idSelector = null)
        {
            await DeleteAsync(new[] { recordId }, idSelector);
        }

        /// <summary>
        /// Delete records matching predicate
        /// </summary>
        public async Task DeleteWhereAsync(Expression<Func<T, bool>> predicate, Func<T, object> idSelector = null)
        {
            var matchingRecords = await FindAsync(predicate);
            if (matchingRecords.Any())
            {
                idSelector ??= _defaultIdSelector;
                var idsToDelete = matchingRecords.Select(idSelector);
                await DeleteAsync(idsToDelete, idSelector);
            }
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Get all records
        /// </summary>
        public async Task<List<T>> GetAllAsync()
        {
            ThrowIfDisposed();

            var allRecords = new List<T>();
            var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();

            foreach (var segmentId in segmentIds)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                allRecords.AddRange(segmentData.Records);
            }

            return allRecords;
        }

        /// <summary>
        /// Get record by ID
        /// </summary>
        public async Task<T> GetByIdAsync(object id, Func<T, object> idSelector = null)
        {
            ThrowIfDisposed();
            idSelector ??= _defaultIdSelector;

            // Try to use index if available
            if (_config.EnableRealTimeIndexing)
            {
                var idPropertyName = GetIdPropertyName();
                if (!string.IsNullOrEmpty(idPropertyName))
                {
                    var indexEntries = await _indexManager.FindByPropertyAsync(idPropertyName, id);
                    if (indexEntries.Any())
                    {
                        var entry = indexEntries.First();
                        var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(entry.SegmentId);
                        return segmentData.Records.FirstOrDefault(r => Equals(idSelector(r), id));
                    }
                }
            }

            // Fallback to sequential search
            var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();
            foreach (var segmentId in segmentIds)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                var record = segmentData.Records.FirstOrDefault(r => Equals(idSelector(r), id));
                if (record != null) return record;
            }

            return null;
        }

        /// <summary>
        /// Find records matching predicate
        /// </summary>
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            ThrowIfDisposed();

            var results = new List<T>();
            var compiledPredicate = predicate.Compile();
            var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();

            foreach (var segmentId in segmentIds)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                var matchingRecords = segmentData.Records.Where(compiledPredicate);
                results.AddRange(matchingRecords);
            }

            return results;
        }

        /// <summary>
        /// Find records by property value using index
        /// </summary>
        public async Task<List<T>> FindByPropertyAsync(string propertyName, object value)
        {
            ThrowIfDisposed();

            if (!_config.EnableRealTimeIndexing)
            {
                throw new InvalidOperationException("Real-time indexing is disabled. Use FindAsync with predicate instead.");
            }

            var indexEntries = await _indexManager.FindByPropertyAsync(propertyName, value);
            var results = new List<T>();

            // Group by segment for efficient loading
            var segmentGroups = indexEntries.GroupBy(e => e.SegmentId);

            foreach (var segmentGroup in segmentGroups)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentGroup.Key);
                var recordIds = segmentGroup.Select(e => e.RecordId).ToHashSet();

                var matchingRecords = segmentData.Records.Where(r =>
                    recordIds.Contains(_defaultIdSelector(r)));
                results.AddRange(matchingRecords);
            }

            return results;
        }

        /// <summary>
        /// Find records by multiple properties using index
        /// </summary>
        public async Task<List<T>> FindByPropertiesAsync(Dictionary<string, object> propertyValues)
        {
            ThrowIfDisposed();

            if (!_config.EnableRealTimeIndexing)
            {
                throw new InvalidOperationException("Real-time indexing is disabled. Use FindAsync with predicate instead.");
            }

            var indexEntries = await _indexManager.FindByPropertiesAsync(propertyValues);
            var results = new List<T>();

            // Group by segment for efficient loading
            var segmentGroups = indexEntries.GroupBy(e => e.SegmentId);

            foreach (var segmentGroup in segmentGroups)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentGroup.Key);
                var recordIds = segmentGroup.Select(e => e.RecordId).ToHashSet();

                var matchingRecords = segmentData.Records.Where(r =>
                    recordIds.Contains(_defaultIdSelector(r)));
                results.AddRange(matchingRecords);
            }

            return results;
        }

        /// <summary>
        /// Find records by property value range using index
        /// </summary>
        public async Task<List<T>> FindByRangeAsync<TValue>(string propertyName, TValue minValue, TValue maxValue)
            where TValue : IComparable<TValue>
        {
            ThrowIfDisposed();

            if (!_config.EnableRealTimeIndexing)
            {
                throw new InvalidOperationException("Real-time indexing is disabled. Use FindAsync with predicate instead.");
            }

            var indexEntries = await _indexManager.FindByRangeAsync(propertyName, minValue, maxValue);
            var results = new List<T>();

            // Group by segment for efficient loading
            var segmentGroups = indexEntries.GroupBy(e => e.SegmentId);

            foreach (var segmentGroup in segmentGroups)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentGroup.Key);
                var recordIds = segmentGroup.Select(e => e.RecordId).ToHashSet();

                var matchingRecords = segmentData.Records.Where(r =>
                    recordIds.Contains(_defaultIdSelector(r)));
                results.AddRange(matchingRecords);
            }

            return results;
        }

        /// <summary>
        /// Get record count
        /// </summary>
        public async Task<long> CountAsync()
        {
            ThrowIfDisposed();

            var metadata = await _metadataManager.GetMetadataAsync();
            return metadata?.TotalRecords ?? 0;
        }

        /// <summary>
        /// Get record count matching predicate
        /// </summary>
        public async Task<long> CountAsync(Expression<Func<T, bool>> predicate)
        {
            ThrowIfDisposed();

            var compiledPredicate = predicate.Compile();
            long count = 0;
            var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();

            foreach (var segmentId in segmentIds)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                count += segmentData.Records.Count(compiledPredicate);
            }

            return count;
        }

        /// <summary>
        /// Check if any records exist
        /// </summary>
        public async Task<bool> AnyAsync()
        {
            return await CountAsync() > 0;
        }

        /// <summary>
        /// Check if any records match predicate
        /// </summary>
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            ThrowIfDisposed();

            var compiledPredicate = predicate.Compile();
            var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();

            foreach (var segmentId in segmentIds)
            {
                var segmentData = await _segmentManager.ReadFromSegmentAsync<T>(segmentId);
                if (segmentData.Records.Any(compiledPredicate))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Bulk insert with better performance for large datasets
        /// </summary>
        public async Task BulkInsertAsync(IEnumerable<T> records, int batchSize = 1000, Func<T, object> idSelector = null)
        {
            ThrowIfDisposed();
            idSelector ??= _defaultIdSelector;

            var recordsList = records.ToList();
            if (!recordsList.Any()) return;

            await _operationLock.WaitAsync();
            try
            {
                // Process in batches
                for (int i = 0; i < recordsList.Count; i += batchSize)
                {
                    var batch = recordsList.Skip(i).Take(batchSize).ToList();
                    var segmentId = await _segmentManager.GetActiveSegmentIdAsync();

                    // Append to current segment
                    await _segmentManager.AppendToSegmentAsync(segmentId, batch);

                    // Update indexes if enabled
                    if (_config.EnableRealTimeIndexing)
                    {
                        await _indexManager.AddToIndexAsync(batch, segmentId, idSelector);
                    }
                }

                await AuditLogger.LogAsync($"Bulk insert completed for {_entityName}",
                    new { TotalRecords = recordsList.Count, BatchSize = batchSize });
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Clear all records from storage
        /// </summary>
        public async Task ClearAsync()
        {
            ThrowIfDisposed();

            await _operationLock.WaitAsync();
            try
            {
                var segmentIds = await _segmentManager.GetAllSegmentIdsAsync();

                foreach (var segmentId in segmentIds)
                {
                    await _segmentManager.WriteToSegmentAsync<T>(segmentId, new List<T>());
                }

                // Clear indexes
                if (_config.EnableRealTimeIndexing)
                {
                    await _indexManager.RebuildIndexesAsync();
                }

                await AuditLogger.LogAsync($"All records cleared from {_entityName}");
            }
            finally
            {
                _operationLock.Release();
            }
        }

        #endregion

        #region Statistics & Maintenance

        /// <summary>
        /// Get storage statistics
        /// </summary>
        public async Task<StorageStatistics> GetStatisticsAsync()
        {
            ThrowIfDisposed();

            var metadata = await _metadataManager.GetMetadataAsync();
            var indexStats = await _indexManager.GetStatisticsAsync();

            var stats = new StorageStatistics
            {
                EntityName = _entityName,
                TotalRecords = metadata?.TotalRecords ?? 0,
                TotalSegments = metadata?.Segments?.Count ?? 0,
                TotalSizeBytes = metadata?.Segments?.Sum(s => s.FileSizeBytes) ?? 0,
                IndexStatistics = indexStats,
                CreatedAt = metadata?.CreatedAt ?? DateTime.MinValue,
                LastModified = metadata?.LastModified ?? DateTime.MinValue
            };

            return stats;
        }

        /// <summary>
        /// Optimize storage (compact segments, rebuild indexes)
        /// </summary>
        public async Task OptimizeAsync()
        {
            ThrowIfDisposed();

            await _operationLock.WaitAsync();
            try
            {
                // Compact segments
                await _segmentManager.CompactSegmentsAsync();

                // Optimize indexes
                if (_config.EnableRealTimeIndexing)
                {
                    await _indexManager.OptimizeIndexesAsync();
                }

                await AuditLogger.LogAsync($"Storage optimization completed for {_entityName}");
            }
            finally
            {
                _operationLock.Release();
            }
        }

        #endregion

        #region Utilities

        private Func<T, object> CreateDefaultIdSelector()
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            // Look for common ID property names
            var idProperty = properties.FirstOrDefault(p =>
                string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, "ID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, $"{type.Name}Id", StringComparison.OrdinalIgnoreCase));

            // Fallback to first property
            idProperty ??= properties.FirstOrDefault();

            if (idProperty == null)
            {
                throw new InvalidOperationException($"No suitable ID property found for type {type.Name}");
            }

            return entity => idProperty.GetValue(entity);
        }

        private string GetIdPropertyName()
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            var idProperty = properties.FirstOrDefault(p =>
                string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, "ID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, $"{type.Name}Id", StringComparison.OrdinalIgnoreCase));

            return idProperty?.Name ?? properties.FirstOrDefault()?.Name;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HierarchicalStorage<T>));
            }
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
                _operationLock?.Dispose();
                _segmentManager?.Dispose();
                _indexManager?.Dispose();
                _metadataManager?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }


}