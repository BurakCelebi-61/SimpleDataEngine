namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Individual validation result
    /// </summary>
    public class ValidationIssue
    {
        public string PropertyName { get; set; }
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
        public ValidationRuleType RuleType { get; set; }
        public object AttemptedValue { get; set; }
        public string RuleName { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}