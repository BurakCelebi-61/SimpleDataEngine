using SimpleDataEngine.Storage.Hierarchical.Managers;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// SegmentManager constructor düzeltmeleri
    /// </summary>
    public static class SegmentManagerFactory
    {
        /// <summary>
        /// SegmentManager oluşturma factory method'u
        /// </summary>
        public static SegmentManager Create(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            return new SegmentManager(config, fileHandler, entityName);
        }

        /// <summary>
        /// Generic SegmentManager oluşturma
        /// </summary>
        public static SegmentManager<T> Create<T>(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName) where T : class
        {
            return new SegmentManager<T>(config, fileHandler, entityName);
        }

        /// <summary>
        /// Async initialization ile SegmentManager oluştur
        /// </summary>
        public static async Task<SegmentManager> CreateAndInitializeAsync(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            var manager = new SegmentManager(config, fileHandler, entityName);
            await manager.InitializeAsync();
            return manager;
        }
    }
}