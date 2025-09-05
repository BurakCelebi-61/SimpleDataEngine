namespace SimpleDataEngine.Backup
{
    /// <summary>
    /// Backup validation result
    /// </summary>
    public class BackupValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}