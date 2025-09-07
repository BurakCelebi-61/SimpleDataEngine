namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Storage information
    /// </summary>
    public class StorageInfo
    {
        public string FilePath { get; set; }
        public bool FileExists { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsEncrypted { get; set; }
        public EncryptionType EncryptionType { get; set; }
        public bool HasIntegrityCheck { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSizeBytes;
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
}