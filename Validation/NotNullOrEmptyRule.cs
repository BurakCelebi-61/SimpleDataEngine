namespace SimpleDataEngine.Validation
{
    internal class NotNullOrEmptyRule<T> : IValidationRule<T>
    {
        private readonly Func<T, string> _propertySelector;
        private readonly string _propertyName;

        public string RuleName => $"NotNullOrEmpty_{_propertyName}";
        public ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public NotNullOrEmptyRule(Func<T, string> propertySelector, string propertyName)
        {
            _propertySelector = propertySelector;
            _propertyName = propertyName;
        }

        public ValidationResult Validate(T entity, ValidationOptions options)
        {
            var result = new ValidationResult { IsValid = true };
            var value = _propertySelector(entity);

            if (string.IsNullOrWhiteSpace(value))
            {
                result.AddError(_propertyName, $"{_propertyName} is required", ValidationRuleType.Required);
            }

            return result;
        }
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}