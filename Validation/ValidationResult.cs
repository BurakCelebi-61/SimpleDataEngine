namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Validation result for an entity
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical);
        public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);
        public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical);
        public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);

        public void AddError(string propertyName, string message, ValidationRuleType ruleType = ValidationRuleType.Custom)
        {
            Issues.Add(new ValidationIssue
            {
                PropertyName = propertyName,
                Message = message,
                Severity = ValidationSeverity.Error,
                RuleType = ruleType
            });
            IsValid = false;
        }

        public void AddWarning(string propertyName, string message, ValidationRuleType ruleType = ValidationRuleType.Custom)
        {
            Issues.Add(new ValidationIssue
            {
                PropertyName = propertyName,
                Message = message,
                Severity = ValidationSeverity.Warning,
                RuleType = ruleType
            });
        }

        public void AddInfo(string propertyName, string message, ValidationRuleType ruleType = ValidationRuleType.Custom)
        {
            Issues.Add(new ValidationIssue
            {
                PropertyName = propertyName,
                Message = message,
                Severity = ValidationSeverity.Info,
                RuleType = ruleType
            });
        }
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}