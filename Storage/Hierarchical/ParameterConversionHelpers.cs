namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// String to parameter conversion helpers
    /// </summary>
    public static class ParameterConversionHelpers
    {
        /// <summary>
        /// HierarchicalDatabaseConfig'den string path çıkarma
        /// </summary>
        public static string GetDataModelsPath(this HierarchicalDatabaseConfig config)
        {
            return config?.DataModelsPath ?? string.Empty;
        }

        /// <summary>
        /// IFileHandler'dan string path çıkarma (eğer file handler path tutar ise)
        /// </summary>
        public static string GetFileExtension(this IFileHandler fileHandler)
        {
            return fileHandler?.FileExtension ?? ".json";
        }

        /// <summary>
        /// Safe parameter extraction for constructor calls
        /// </summary>
        public static (IFileHandler fileHandler, string path) ExtractParameters(HierarchicalDatabaseConfig config)
        {
            IFileHandler fileHandler;

            if (config.EncryptionEnabled && config.Encryption != null)
            {
                fileHandler = new EncryptedFileHandler(config.Encryption);
            }
            else
            {
                fileHandler = new StandardFileHandler();
            }

            return (fileHandler, config.DataModelsPath);
        }

        /// <summary>
        /// Safe parameter extraction for manager constructors
        /// </summary>
        public static (HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
            ExtractManagerParameters(HierarchicalDatabaseConfig config, IFileHandler fileHandler, string entityName)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (fileHandler == null) throw new ArgumentNullException(nameof(fileHandler));
            if (string.IsNullOrEmpty(entityName)) throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));

            return (config, fileHandler, entityName);
        }
    }
}