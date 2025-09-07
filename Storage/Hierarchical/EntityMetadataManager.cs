using SimpleDataEngine.Storage.Hierarchical.Models;
using System.Text.Json;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Entity metadata manager for entity-specific operations
    /// </summary>
    public class EntityMetadataManager : IDisposable
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly string _entityName;
        private EntityMetadata _metadata;
        private bool _disposed;

        public EntityMetadataManager(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        }

        public async Task InitializeAsync()
        {
            var metadataPath = GetMetadataPath();
            var content = await _fileHandler.ReadTextAsync(metadataPath);

            if (string.IsNullOrEmpty(content))
            {
                _metadata = new EntityMetadata { EntityName = _entityName };
                await SaveMetadataAsync(_metadata);
            }
            else
            {
                _metadata = JsonSerializer.Deserialize<EntityMetadata>(content);
            }
        }

        public async Task<EntityMetadata> GetMetadataAsync()
        {
            return _metadata ?? new EntityMetadata { EntityName = _entityName };
        }

        public async Task SaveMetadataAsync(EntityMetadata metadata)
        {
            var metadataPath = GetMetadataPath();
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await _fileHandler.WriteTextAsync(metadataPath, json);
            _metadata = metadata;
        }

        private string GetMetadataPath()
        {
            var entityPath = Path.Combine(_config.DataModelsPath, _entityName);
            return Path.Combine(entityPath, $"metadata{_config.FileExtension}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}