namespace SimpleDataEngine.Export
{
    /// <summary>
    /// Export result information
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string ExportPath { get; set; }
        public long FileSizeBytes { get; set; }
        public int TotalRecords { get; set; }
        public Dictionary<string, int> RecordCounts { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

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