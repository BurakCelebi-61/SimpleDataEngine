namespace SimpleDataEngine.Validation
{
    internal class StringLengthRule<T> : IValidationRule<T>
    {
        private readonly Func<T, string> _propertySelector;
        private readonly string _propertyName;
        private readonly int _minLength;
        private readonly int _maxLength;

        public string RuleName => $"StringLength_{_propertyName}";
        public ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public StringLengthRule(Func<T, string> propertySelector, string propertyName, int minLength, int maxLength)
        {
            _propertySelector = propertySelector;
            _propertyName = propertyName;
            _minLength = minLength;
            _maxLength = maxLength;
        }

        public ValidationResult Validate(T entity, ValidationOptions options)
        {
            var result = new ValidationResult { IsValid = true };
            var value = _propertySelector(entity) ?? "";

            if (value.Length < _minLength)
            {
                result.AddError(_propertyName,
                    $"{_propertyName} must be at least {_minLength} characters long. Current length: {value.Length}",
                    ValidationRuleType.StringLength);
            }
            else if (value.Length > _maxLength)
            {
                result.AddError(_propertyName,
                    $"{_propertyName} cannot exceed {_maxLength} characters. Current length: {value.Length}",
                    ValidationRuleType.StringLength);
            }

            return result;
        }
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}