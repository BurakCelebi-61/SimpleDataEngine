namespace SimpleDataEngine.Backup
{
    /// <summary>
    /// Backup information
    /// </summary>
    public class BackupInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public long FileSizeBytes { get; set; }
        public string Description { get; set; }
        public bool IsCompressed { get; set; }
        public string Version { get; set; }
        public TimeSpan Age => DateTime.Now - CreatedAt;
        public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}