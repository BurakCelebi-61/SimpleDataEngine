using SimpleDataEngine.Storage.Hierarchical.Managers;
using SimpleDataEngine.Storage.Hierarchical.Models;
using SimpleDataEngine.Storage.Hierarchical.SimpleDataEngine.Storage.Hierarchical.Managers;
using System.Collections.Concurrent;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// HierarchicalDatabase constructor parameter hatalarının düzeltilmesi
    /// </summary>
    public partial class HierarchicalDatabase
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly GlobalMetadataManager _globalMetadataManager;
        private readonly ConcurrentDictionary<string, EntityMetadataManager> _entityManagers;
        private readonly ConcurrentDictionary<string, SegmentManager> _segmentManagers;
        private readonly SemaphoreSlim _initializationLock;
        private bool _isInitialized;
        private bool _disposed;
        /// <summary>
        /// Düzeltilmiş constructor - config ve fileHandler parametre sırası
        /// </summary>
        public HierarchicalDatabase(HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _globalMetadataManager = new GlobalMetadataManager(_fileHandler, _config.DataModelsPath);
            _entityManagers = new ConcurrentDictionary<string, EntityMetadataManager>();
            _segmentManagers = new ConcurrentDictionary<string, SegmentManager>();
            _initializationLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Alternative constructor - sadece config ile
        /// </summary>
        public HierarchicalDatabase(HierarchicalDatabaseConfig config)
            : this(config, CreateFileHandler(config))
        {
        }


        /// <summary>
        /// Factory method for file handler creation
        /// </summary>
        private static IFileHandler CreateFileHandler(HierarchicalDatabaseConfig config)
        {
            if (config.EncryptionEnabled)
            {
                return new EncryptedFileHandler(config.Encryption);
            }
            else
            {
                return new StandardFileHandler();
            }
        }
        private async Task EnsureEntityManagerAsync(string entityName)
        {
            if (_entityManagers.ContainsKey(entityName)) return;

            var entityManager = new EntityMetadataManager(_config, _fileHandler, entityName);
            await entityManager.InitializeAsync();

            var segmentManager = new SegmentManager(_config, _fileHandler, entityName);
            await segmentManager.InitializeAsync();

            _entityManagers.TryAdd(entityName, entityManager);
            _segmentManagers.TryAdd(entityName, segmentManager);
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _initializationLock?.Dispose();
                foreach (var manager in _entityManagers.Values)
                    manager?.Dispose();
                foreach (var manager in _segmentManagers.Values)
                    manager?.Dispose();
                _disposed = true;
            }
        }
    }
}