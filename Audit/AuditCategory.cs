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
}