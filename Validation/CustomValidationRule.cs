namespace SimpleDataEngine.Validation
{
    internal class CustomValidationRule<T> : IValidationRule<T>
    {
        private readonly Func<T, ValidationOptions, ValidationResult> _validator;

        public string RuleName { get; }
        public ValidationSeverity DefaultSeverity { get; }

        public CustomValidationRule(string ruleName, Func<T, ValidationOptions, ValidationResult> validator, ValidationSeverity severity)
        {
            RuleName = ruleName;
            _validator = validator;
            DefaultSeverity = severity;
        }

        public ValidationResult Validate(T entity, ValidationOptions options)
        {
            return _validator(entity, options);
        }
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}