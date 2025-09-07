namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit event categories - Güncel ve Kapsamlı
    /// </summary>
    public enum AuditCategory
    {
        /// <summary>
        /// Genel sistem olayları
        /// </summary>
        System = 0,

        /// <summary>
        /// Güvenlik olayları (authentication, authorization, vb.)
        /// </summary>
        Security = 1,

        /// <summary>
        /// Veri işlemleri (CRUD, queries, vb.)
        /// </summary>
        Data = 2,

        /// <summary>
        /// Performans metrikleri ve monitoring
        /// </summary>
        Performance = 3,

        /// <summary>
        /// Konfigürasyon değişiklikleri
        /// </summary>
        Configuration = 4,

        /// <summary>
        /// Backup ve restore işlemleri
        /// </summary>
        Backup = 5,

        /// <summary>
        /// Export ve import işlemleri
        /// </summary>
        Export = 6,

        /// <summary>
        /// Veri doğrulama ve validation
        /// </summary>
        Validation = 7,

        /// <summary>
        /// Sistem sağlığı kontrolü
        /// </summary>
        Health = 8,

        /// <summary>
        /// Kullanıcı tanımlı kategoriler
        /// </summary>
        Custom = 9,

        /// <summary>
        /// Genel kategori (backward compatibility)
        /// </summary>
        General = 10,

        /// <summary>
        /// Hata kategorisi (backward compatibility)
        /// </summary>
        Error = 11,

        /// <summary>
        /// Debug kategorisi (backward compatibility)
        /// </summary>
        Debug = 12,

        /// <summary>
        /// API çağrıları ve web istekleri
        /// </summary>
        Api = 13,

        /// <summary>
        /// Database işlemleri
        /// </summary>
        Database = 14,

        /// <summary>
        /// File system işlemleri
        /// </summary>
        FileSystem = 15,

        /// <summary>
        /// Network işlemleri
        /// </summary>
        Network = 16,

        /// <summary>
        /// Authentication işlemleri
        /// </summary>
        Authentication = 17,

        /// <summary>
        /// Authorization işlemleri
        /// </summary>
        Authorization = 18,

        /// <summary>
        /// Session yönetimi
        /// </summary>
        Session = 19,

        /// <summary>
        /// Cache işlemleri
        /// </summary>
        Cache = 20,

        /// <summary>
        /// Encryption/Decryption işlemleri
        /// </summary>
        Encryption = 21,

        /// <summary>
        /// Index işlemleri
        /// </summary>
        Indexing = 22,

        /// <summary>
        /// Segment işlemleri
        /// </summary>
        Segmentation = 23,

        /// <summary>
        /// Compaction işlemleri
        /// </summary>
        Compaction = 24,

        /// <summary>
        /// Migration işlemleri
        /// </summary>
        Migration = 25,

        /// <summary>
        /// Maintenance işlemleri
        /// </summary>
        Maintenance = 26,

        /// <summary>
        /// Transaction işlemleri
        /// </summary>
        Transaction = 27,

        /// <summary>
        /// Monitoring ve alerting
        /// </summary>
        Monitoring = 28,

        /// <summary>
        /// Business logic olayları
        /// </summary>
        Business = 29,

        /// <summary>
        /// Integration olayları
        /// </summary>
        Integration = 30
    }

    /// <summary>
    /// AuditCategory utility methods
    /// </summary>
    public static class AuditCategoryExtensions
    {
        /// <summary>
        /// Category'nin renk kodunu al (UI için)
        /// </summary>
        public static ConsoleColor GetConsoleColor(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.System => ConsoleColor.Blue,
                AuditCategory.Security or AuditCategory.Authentication or AuditCategory.Authorization => ConsoleColor.Magenta,
                AuditCategory.Data or AuditCategory.Database => ConsoleColor.Green,
                AuditCategory.Performance or AuditCategory.Monitoring => ConsoleColor.Yellow,
                AuditCategory.Configuration => ConsoleColor.Cyan,
                AuditCategory.Backup or AuditCategory.Export => ConsoleColor.DarkGreen,
                AuditCategory.Validation => ConsoleColor.DarkYellow,
                AuditCategory.Health => ConsoleColor.DarkCyan,
                AuditCategory.Error => ConsoleColor.Red,
                AuditCategory.Debug => ConsoleColor.Gray,
                AuditCategory.Api or AuditCategory.Network => ConsoleColor.DarkBlue,
                AuditCategory.FileSystem => ConsoleColor.DarkMagenta,
                AuditCategory.Encryption => ConsoleColor.DarkRed,
                AuditCategory.Business => ConsoleColor.White,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Category'nin kısa ismini al
        /// </summary>
        public static string GetShortName(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.System => "SYS",
                AuditCategory.Security => "SEC",
                AuditCategory.Data => "DAT",
                AuditCategory.Performance => "PRF",
                AuditCategory.Configuration => "CFG",
                AuditCategory.Backup => "BCK",
                AuditCategory.Export => "EXP",
                AuditCategory.Validation => "VAL",
                AuditCategory.Health => "HTH",
                AuditCategory.Custom => "CST",
                AuditCategory.General => "GEN",
                AuditCategory.Error => "ERR",
                AuditCategory.Debug => "DBG",
                AuditCategory.Api => "API",
                AuditCategory.Database => "DB",
                AuditCategory.FileSystem => "FS",
                AuditCategory.Network => "NET",
                AuditCategory.Authentication => "AUTH",
                AuditCategory.Authorization => "AUTHZ",
                AuditCategory.Session => "SESS",
                AuditCategory.Cache => "CACHE",
                AuditCategory.Encryption => "ENC",
                AuditCategory.Indexing => "IDX",
                AuditCategory.Segmentation => "SEG",
                AuditCategory.Compaction => "COMP",
                AuditCategory.Migration => "MIG",
                AuditCategory.Maintenance => "MAINT",
                AuditCategory.Transaction => "TXN",
                AuditCategory.Monitoring => "MON",
                AuditCategory.Business => "BIZ",
                AuditCategory.Integration => "INT",
                _ => "UNK"
            };
        }

        /// <summary>
        /// Category'nin açıklamasını al
        /// </summary>
        public static string GetDescription(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.System => "System-level events and operations",
                AuditCategory.Security => "Security-related events and violations",
                AuditCategory.Data => "Data manipulation and query operations",
                AuditCategory.Performance => "Performance metrics and monitoring",
                AuditCategory.Configuration => "Configuration changes and updates",
                AuditCategory.Backup => "Backup and restore operations",
                AuditCategory.Export => "Data export and import operations",
                AuditCategory.Validation => "Data validation and integrity checks",
                AuditCategory.Health => "System health monitoring",
                AuditCategory.Custom => "User-defined custom events",
                AuditCategory.General => "General application events",
                AuditCategory.Error => "Error events and exceptions",
                AuditCategory.Debug => "Debug and troubleshooting information",
                AuditCategory.Api => "API calls and web requests",
                AuditCategory.Database => "Database operations and queries",
                AuditCategory.FileSystem => "File system operations",
                AuditCategory.Network => "Network communications",
                AuditCategory.Authentication => "User authentication events",
                AuditCategory.Authorization => "Access control and permissions",
                AuditCategory.Session => "Session management events",
                AuditCategory.Cache => "Cache operations and management",
                AuditCategory.Encryption => "Encryption and decryption operations",
                AuditCategory.Indexing => "Index creation and maintenance",
                AuditCategory.Segmentation => "Data segmentation operations",
                AuditCategory.Compaction => "Data compaction and optimization",
                AuditCategory.Migration => "Data migration operations",
                AuditCategory.Maintenance => "System maintenance tasks",
                AuditCategory.Transaction => "Transaction management",
                AuditCategory.Monitoring => "System monitoring and alerting",
                AuditCategory.Business => "Business logic events",
                AuditCategory.Integration => "External system integration",
                _ => "Unknown category"
            };
        }

        /// <summary>
        /// Category güvenlik kritik mi kontrol et
        /// </summary>
        public static bool IsSecurityCritical(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.Security or
                AuditCategory.Authentication or
                AuditCategory.Authorization or
                AuditCategory.Encryption => true,
                _ => false
            };
        }

        /// <summary>
        /// Category performance related mi kontrol et
        /// </summary>
        public static bool IsPerformanceRelated(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.Performance or
                AuditCategory.Monitoring or
                AuditCategory.Health or
                AuditCategory.Cache => true,
                _ => false
            };
        }

        /// <summary>
        /// Category data related mi kontrol et
        /// </summary>
        public static bool IsDataRelated(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.Data or
                AuditCategory.Database or
                AuditCategory.Backup or
                AuditCategory.Export or
                AuditCategory.Validation or
                AuditCategory.Indexing or
                AuditCategory.Segmentation or
                AuditCategory.Compaction or
                AuditCategory.Migration => true,
                _ => false
            };
        }

        /// <summary>
        /// String'den AuditCategory parse et
        /// </summary>
        public static AuditCategory ParseCategory(string categoryString)
        {
            if (string.IsNullOrWhiteSpace(categoryString))
                return AuditCategory.General;

            return categoryString.ToUpperInvariant() switch
            {
                "SYS" or "SYSTEM" => AuditCategory.System,
                "SEC" or "SECURITY" => AuditCategory.Security,
                "DAT" or "DATA" => AuditCategory.Data,
                "PRF" or "PERFORMANCE" => AuditCategory.Performance,
                "CFG" or "CONFIG" or "CONFIGURATION" => AuditCategory.Configuration,
                "BCK" or "BACKUP" => AuditCategory.Backup,
                "EXP" or "EXPORT" => AuditCategory.Export,
                "VAL" or "VALIDATION" => AuditCategory.Validation,
                "HTH" or "HEALTH" => AuditCategory.Health,
                "CST" or "CUSTOM" => AuditCategory.Custom,
                "GEN" or "GENERAL" => AuditCategory.General,
                "ERR" or "ERROR" => AuditCategory.Error,
                "DBG" or "DEBUG" => AuditCategory.Debug,
                "API" => AuditCategory.Api,
                "DB" or "DATABASE" => AuditCategory.Database,
                "FS" or "FILESYSTEM" => AuditCategory.FileSystem,
                "NET" or "NETWORK" => AuditCategory.Network,
                "AUTH" or "AUTHENTICATION" => AuditCategory.Authentication,
                "AUTHZ" or "AUTHORIZATION" => AuditCategory.Authorization,
                "SESS" or "SESSION" => AuditCategory.Session,
                "CACHE" => AuditCategory.Cache,
                "ENC" or "ENCRYPTION" => AuditCategory.Encryption,
                "IDX" or "INDEX" or "INDEXING" => AuditCategory.Indexing,
                "SEG" or "SEGMENT" or "SEGMENTATION" => AuditCategory.Segmentation,
                "COMP" or "COMPACTION" => AuditCategory.Compaction,
                "MIG" or "MIGRATION" => AuditCategory.Migration,
                "MAINT" or "MAINTENANCE" => AuditCategory.Maintenance,
                "TXN" or "TRANSACTION" => AuditCategory.Transaction,
                "MON" or "MONITORING" => AuditCategory.Monitoring,
                "BIZ" or "BUSINESS" => AuditCategory.Business,
                "INT" or "INTEGRATION" => AuditCategory.Integration,
                _ => Enum.TryParse<AuditCategory>(categoryString, true, out var parsed) ? parsed : AuditCategory.General
            };
        }

        /// <summary>
        /// Category grubu al (high-level grouping için)
        /// </summary>
        public static string GetCategoryGroup(this AuditCategory category)
        {
            return category switch
            {
                AuditCategory.System or AuditCategory.Health or AuditCategory.Configuration => "System",
                AuditCategory.Security or AuditCategory.Authentication or AuditCategory.Authorization or AuditCategory.Encryption => "Security",
                AuditCategory.Data or AuditCategory.Database or AuditCategory.Indexing or AuditCategory.Segmentation => "Data",
                AuditCategory.Performance or AuditCategory.Monitoring or AuditCategory.Cache => "Performance",
                AuditCategory.Backup or AuditCategory.Export or AuditCategory.Migration => "Operations",
                AuditCategory.Api or AuditCategory.Network or AuditCategory.Integration => "Communication",
                AuditCategory.Business or AuditCategory.Custom => "Application",
                _ => "General"
            };
        }
    }
}