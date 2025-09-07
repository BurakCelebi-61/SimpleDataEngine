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

    /// <summary>
    /// AuditLevel utility methods
    /// </summary>
    public static class AuditLevelExtensions
    {
        /// <summary>
        /// Log level'ın renk kodunu al (console logging için)
        /// </summary>
        public static ConsoleColor GetConsoleColor(this AuditLevel level)
        {
            return level switch
            {
                AuditLevel.Trace => ConsoleColor.DarkGray,
                AuditLevel.Debug => ConsoleColor.Gray,
                AuditLevel.Info or AuditLevel.Information => ConsoleColor.White,
                AuditLevel.Warning => ConsoleColor.Yellow,
                AuditLevel.Error => ConsoleColor.Red,
                AuditLevel.Critical => ConsoleColor.Magenta,
                AuditLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Log level'ın kısa ismini al
        /// </summary>
        public static string GetShortName(this AuditLevel level)
        {
            return level switch
            {
                AuditLevel.Trace => "TRC",
                AuditLevel.Debug => "DBG",
                AuditLevel.Info or AuditLevel.Information => "INF",
                AuditLevel.Warning => "WRN",
                AuditLevel.Error => "ERR",
                AuditLevel.Critical => "CRT",
                AuditLevel.Fatal => "FTL",
                _ => "UNK"
            };
        }

        /// <summary>
        /// Log level'ın öncelik değerini al
        /// </summary>
        public static int GetPriority(this AuditLevel level)
        {
            return (int)level;
        }

        /// <summary>
        /// Log level kritik mi kontrol et
        /// </summary>
        public static bool IsCritical(this AuditLevel level)
        {
            return level >= AuditLevel.Critical;
        }

        /// <summary>
        /// Log level hata seviyesi mi kontrol et
        /// </summary>
        public static bool IsError(this AuditLevel level)
        {
            return level >= AuditLevel.Error;
        }

        /// <summary>
        /// Log level uyarı seviyesi mi kontrol et
        /// </summary>
        public static bool IsWarning(this AuditLevel level)
        {
            return level >= AuditLevel.Warning;
        }

        /// <summary>
        /// String'den AuditLevel parse et
        /// </summary>
        public static AuditLevel ParseLevel(string levelString)
        {
            if (string.IsNullOrWhiteSpace(levelString))
                return AuditLevel.Info;

            return levelString.ToUpperInvariant() switch
            {
                "TRACE" or "TRC" => AuditLevel.Trace,
                "DEBUG" or "DBG" => AuditLevel.Debug,
                "INFO" or "INF" or "INFORMATION" => AuditLevel.Info,
                "WARNING" or "WARN" or "WRN" => AuditLevel.Warning,
                "ERROR" or "ERR" => AuditLevel.Error,
                "CRITICAL" or "CRT" or "CRIT" => AuditLevel.Critical,
                "FATAL" or "FTL" => AuditLevel.Fatal,
                _ => Enum.TryParse<AuditLevel>(levelString, true, out var parsed) ? parsed : AuditLevel.Info
            };
        }

        /// <summary>
        /// Minimum level kontrolü
        /// </summary>
        public static bool ShouldLog(this AuditLevel level, AuditLevel minimumLevel)
        {
            return level >= minimumLevel;
        }
    }
}