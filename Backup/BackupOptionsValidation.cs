namespace SimpleDataEngine.Backup
{
    /// <summary>
    /// Backup options validation result
    /// </summary>
    public class BackupOptionsValidation
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}