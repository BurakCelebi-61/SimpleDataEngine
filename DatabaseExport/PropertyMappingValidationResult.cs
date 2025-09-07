namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Property mapping validation result
    /// </summary>
    public class PropertyMappingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
    }
}