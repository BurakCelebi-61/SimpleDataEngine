namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Validation options and settings
    /// </summary>
    public class ValidationOptions
    {
        /// <summary>
        /// Whether to validate data annotations
        /// </summary>
        public bool ValidateDataAnnotations { get; set; } = true;

        /// <summary>
        /// Whether to validate business rules
        /// </summary>
        public bool ValidateBusinessRules { get; set; } = true;

        /// <summary>
        /// Whether to validate referential integrity
        /// </summary>
        public bool ValidateReferentialIntegrity { get; set; } = true;

        /// <summary>
        /// Whether to continue validation after first error
        /// </summary>
        public bool StopOnFirstError { get; set; } = false;

        /// <summary>
        /// Maximum number of validation issues to collect
        /// </summary>
        public int MaxIssues { get; set; } = 100;

        /// <summary>
        /// Whether to include warnings in validation
        /// </summary>
        public bool IncludeWarnings { get; set; } = true;

        /// <summary>
        /// Custom validation context data
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}