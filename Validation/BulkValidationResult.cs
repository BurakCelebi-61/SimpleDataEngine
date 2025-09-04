namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Bulk validation result for multiple entities
    /// </summary>
    public class BulkValidationResult
    {
        public bool IsValid { get; set; } = true;
        public Dictionary<object, ValidationResult> Results { get; set; } = new();
        public int TotalProcessed { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public TimeSpan Duration { get; set; }

        public List<ValidationIssue> AllIssues => Results.SelectMany(r => r.Value.Issues).ToList();
        public bool HasAnyErrors => Results.Any(r => r.Value.HasErrors);
        public int TotalErrors => Results.Sum(r => r.Value.ErrorCount);
        public int TotalWarnings => Results.Sum(r => r.Value.WarningCount);
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}