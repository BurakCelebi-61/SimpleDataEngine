namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit log levels - Güncel ve Kapsamlı
    /// </summary>
    public enum AuditLevel
    {
        /// <summary>
        /// En detaylı seviye - sadece development için
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Debug bilgileri - development ve troubleshooting için
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Genel bilgi mesajları - normal operasyonlar
        /// </summary>
        Info = 2,

        /// <summary>
        /// Alternatif Info tanımı (backward compatibility)
        /// </summary>
        Information = 2,

        /// <summary>
        /// Uyarı mesajları - dikkat gerektiren durumlar
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Hata mesajları - işlem başarısız oldu ama sistem devam ediyor
        /// </summary>
        Error = 4,

        /// <summary>
        /// Kritik hatalar - sistem kararsızlığı, acil müdahale gerekli
        /// </summary>
        Critical = 5,

        /// <summary>
        /// Fatal hatalar - sistem çökmesi, uygulama durmalı
        /// </summary>
        Fatal = 6
    }
}