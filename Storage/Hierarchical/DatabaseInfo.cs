namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Database information structure
    /// </summary>
    public class DatabaseInfo
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public long TotalSizeBytes { get; set; }
        public int EntityCount { get; set; }
        public string? Error { get; set; }

        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:F2} GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F2} KB";
            return $"{bytes} bytes";
        }
    }
}