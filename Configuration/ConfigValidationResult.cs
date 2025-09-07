namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
    }
}