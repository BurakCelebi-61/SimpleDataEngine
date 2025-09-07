using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// GlobalMetadataManager için eksik method'ların extension implementasyonu
    /// </summary>
    public static class GlobalMetadataManagerExtensions
    {
        /// <summary>
        /// InitializeAsync method - hata listesinde eksik olan method
        /// </summary>
        public static async Task InitializeAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                // Global metadata'yı yükle veya oluştur
                var metadata = await manager.LoadAsync();

                if (metadata == null)
                {
                    // Yeni metadata oluştur
                    metadata = new GlobalMetadata
                    {
                        DatabaseVersion = "1.0.0",
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow,
                        TotalEntities = 0,
                        TotalRecords = 0,
                        TotalSizeMB = 0,
                        Entities = new List<GlobalMetadata.EntityInfo>()
                    };

                    await manager.SaveAsync(metadata);
                }

                // Metadata dosyası var mı kontrol et
                var exists = await manager.ExistsAsync();
                if (!exists)
                {
                    await manager.SaveAsync(metadata);
                }

                await AuditLogger.LogAsync("GlobalMetadataManager initialized successfully",
                    new { MetadataPath = manager.GetMetadataPath(), TotalEntities = metadata.TotalEntities });
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to initialize GlobalMetadataManager", ex);
                throw;
            }
        }

        /// <summary>
        /// GetMetadataAsync method - hata listesinde eksik olan method
        /// </summary>
        public static async Task<GlobalMetadata> GetMetadataAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                return await manager.LoadAsync();
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to get metadata from GlobalMetadataManager", ex);
                throw;
            }
        }

        /// <summary>
        /// UpdateEntityRegistrationAsync method - hata listesinde eksik olan method
        /// </summary>
        public static async Task UpdateEntityRegistrationAsync(this GlobalMetadataManager manager, string entityName, Type entityType)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (string.IsNullOrEmpty(entityName)) throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            try
            {
                var metadata = await manager.LoadAsync();

                // Entity'nin zaten kayıtlı olup olmadığını kontrol et
                var existingEntity = metadata.Entities.FirstOrDefault(e => e.Name == entityName);

                if (existingEntity == null)
                {
                    // Yeni entity ekle
                    var newEntity = new GlobalMetadata.EntityInfo
                    {
                        Name = entityName,
                        RecordCount = 0,
                        SegmentCount = 0,
                        SizeMB = 0,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow,
                        IsActive = true
                    };

                    metadata.Entities.Add(newEntity);
                    metadata.TotalEntities = metadata.Entities.Count;
                    metadata.LastUpdated = DateTime.UtcNow;

                    await manager.SaveAsync(metadata);

                    await AuditLogger.LogAsync($"Entity '{entityName}' registered successfully",
                        new { EntityName = entityName, EntityType = entityType.Name });
                }
                else
                {
                    // Mevcut entity'yi güncelle
                    existingEntity.LastUpdated = DateTime.UtcNow;
                    existingEntity.IsActive = true;

                    await manager.SaveAsync(metadata);

                    await AuditLogger.LogAsync($"Entity '{entityName}' registration updated",
                        new { EntityName = entityName, EntityType = entityType.Name });
                }
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync($"Failed to update entity registration for '{entityName}'", ex);
                throw;
            }
        }

        /// <summary>
        /// UpdateMaintenanceInfoAsync method - hata listesinde eksik olan method
        /// </summary>
        public static async Task UpdateMaintenanceInfoAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                var metadata = await manager.LoadAsync();

                // Maintenance bilgilerini güncelle
                metadata.LastUpdated = DateTime.UtcNow;

                // ConfigSnapshot içinde maintenance bilgisi yoksa ekle
                if (metadata.ConfigSnapshot == null)
                {
                    metadata.ConfigSnapshot = new GlobalMetadata.DatabaseConfigSnapshot();
                }

                await manager.SaveAsync(metadata);

                await AuditLogger.LogAsync("Database maintenance info updated",
                    new { LastUpdated = metadata.LastUpdated });
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to update maintenance info", ex);
                throw;
            }
        }

        /// <summary>
        /// RefreshEntityStatisticsAsync method - entity istatistiklerini yenile
        /// </summary>
        public static async Task RefreshEntityStatisticsAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                var metadata = await manager.LoadAsync();

                // Tüm entity'lerin istatistiklerini yeniden hesapla
                long totalRecords = 0;
                long totalSizeMB = 0;

                foreach (var entity in metadata.Entities)
                {
                    totalRecords += entity.RecordCount;
                    totalSizeMB += entity.SizeMB;
                }

                metadata.TotalRecords = totalRecords;
                metadata.TotalSizeMB = totalSizeMB;
                metadata.TotalEntities = metadata.Entities.Count;
                metadata.LastUpdated = DateTime.UtcNow;

                await manager.SaveAsync(metadata);

                await AuditLogger.LogAsync("Entity statistics refreshed",
                    new
                    {
                        TotalEntities = metadata.TotalEntities,
                        TotalRecords = metadata.TotalRecords,
                        TotalSizeMB = metadata.TotalSizeMB
                    });
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to refresh entity statistics", ex);
                throw;
            }
        }

        /// <summary>
        /// GetEntityStatisticsAsync method - entity istatistiklerini getir
        /// </summary>
        public static async Task<Dictionary<string, GlobalMetadata.EntityInfo>> GetEntityStatisticsAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                var metadata = await manager.LoadAsync();
                return metadata.Entities.ToDictionary(e => e.Name, e => e);
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to get entity statistics", ex);
                throw;
            }
        }

        /// <summary>
        /// ValidateIntegrityAsync method - metadata bütünlüğünü kontrol et
        /// </summary>
        public static async Task<ValidationResult> ValidateIntegrityAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                return await manager.ValidateAsync();
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to validate metadata integrity", ex);

                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"Validation failed: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// RepairMetadataAsync method - metadata onarımı
        /// </summary>
        public static async Task<bool> RepairMetadataAsync(this GlobalMetadataManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            try
            {
                var validation = await manager.ValidateIntegrityAsync();

                if (validation.IsValid)
                {
                    await AuditLogger.LogAsync("Metadata is valid, no repair needed");
                    return true;
                }

                // Backup oluştur
                var backupPath = manager.GetMetadataPath() + $".backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                await manager.CreateBackupAsync(backupPath);

                // Metadata'yı yeniden oluştur
                var metadata = await manager.LoadAsync() ?? new GlobalMetadata();

                // Temel değerleri düzelt
                if (string.IsNullOrEmpty(metadata.DatabaseVersion))
                    metadata.DatabaseVersion = "1.0.0";

                if (metadata.CreatedAt == DateTime.MinValue)
                    metadata.CreatedAt = DateTime.UtcNow;

                metadata.LastUpdated = DateTime.UtcNow;

                // Entity listesi null ise oluştur
                metadata.Entities ??= new List<GlobalMetadata.EntityInfo>();

                // İstatistikleri yeniden hesapla
                metadata.TotalEntities = metadata.Entities.Count;
                metadata.TotalRecords = metadata.Entities.Sum(e => e.RecordCount);
                metadata.TotalSizeMB = metadata.Entities.Sum(e => e.SizeMB);

                await manager.SaveAsync(metadata);

                await AuditLogger.LogAsync("Metadata repair completed successfully",
                    new { BackupPath = backupPath, TotalEntities = metadata.TotalEntities });

                return true;
            }
            catch (Exception ex)
            {
                await AuditLogger.LogErrorAsync("Failed to repair metadata", ex);
                return false;
            }
        }
    }
}