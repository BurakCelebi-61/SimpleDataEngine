using SimpleDataEngine.Storage.Hierarchical.SimpleDataEngine.Storage.Hierarchical.Managers;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// EntityMetadataManager constructor düzeltmeleri
    /// </summary>
    public static class EntityMetadataManagerFactory
    {
        /// <summary>
        /// EntityMetadataManager oluşturma factory method'u
        /// </summary>
        public static EntityMetadataManager Create(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            return new EntityMetadataManager(config, fileHandler, entityName);
        }

        /// <summary>
        /// Async initialization ile EntityMetadataManager oluştur
        /// </summary>
        public static async Task<EntityMetadataManager> CreateAndInitializeAsync(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            var manager = new EntityMetadataManager(config, fileHandler, entityName);
            await manager.InitializeAsync();
            return manager;
        }
    }
}