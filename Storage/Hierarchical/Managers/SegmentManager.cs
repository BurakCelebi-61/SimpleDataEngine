using SimpleDataEngine.Audit;
using SimpleDataEngine.Storage.Hierarchical.Models;
using SimpleDataEngine.Storage.Hierarchical.SimpleDataEngine.Storage.Hierarchical.Managers;
using System.Collections.Concurrent;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    public class SegmentManager : IDisposable
    {
        // Class should NOT be generic
        // Generic methods are fine, but class itself should not have <T>

        // Existing fields and constructor
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly string _entityName;
        private readonly string _entityPath;
        private readonly EntityMetadataManager _entityMetadataManager;
        private readonly ConcurrentDictionary<int, SegmentMetadata> _segmentCache;
        private readonly SemaphoreSlim _segmentLock;
        private bool _disposed;

        public SegmentManager(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _entityPath = Path.Combine(config.DataModelsPath, entityName);
            _entityMetadataManager = new EntityMetadataManager(config, fileHandler, entityName);
            _segmentCache = new ConcurrentDictionary<int, SegmentMetadata>();
            _segmentLock = new SemaphoreSlim(1, 1);
        }

        // Generic methods are OK
        public async Task<SegmentData<T>> ReadFromSegmentAsync<T>(int segmentId) where T : class
        {
            // Implementation
            var filePath = GetSegmentFilePath(segmentId);
            if (!File.Exists(filePath))
            {
                return new SegmentData<T> { SegmentId = segmentId, Records = new List<T>() };
            }

            try
            {
                var segmentData = await _fileHandler.ReadAsync<SegmentData<T>>(filePath);
                return segmentData ?? new SegmentData<T> { SegmentId = segmentId, Records = new List<T>() };
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to read from segment {segmentId}", ex);
                throw;
            }
        }

        private string GetSegmentFilePath(int segmentId)
        {
            var fileName = $"{_entityName}_segment_{segmentId:D6}{_config.FileExtension}";
            return Path.Combine(_entityPath, fileName);
        }

        private async Task UpdateSegmentMetadataAfterWriteAsync(int segmentId, int recordCount, string filePath)
        {
            // Implementation
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_entityPath))
            {
                Directory.CreateDirectory(_entityPath);
            }
            await _entityMetadataManager.InitializeAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _segmentLock?.Dispose();
                _entityMetadataManager?.Dispose();
                _disposed = true;
            }
        }
    }
}