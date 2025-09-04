namespace SimpleDataEngine.Validation
{
    internal class RangeRule<T> : IValidationRule<T>
    {
        private readonly Func<T, decimal> _propertySelector;
        private readonly string _propertyName;
        private readonly decimal _min;
        private readonly decimal _max;

        public string RuleName => $"Range_{_propertyName}";
        public ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public RangeRule(Func<T, decimal> propertySelector, string propertyName, decimal min, decimal max)
        {
            _propertySelector = propertySelector;
            _propertyName = propertyName;
            _min = min;
            _max = max;
        }

        public ValidationResult Validate(T entity, ValidationOptions options)
        {
            var result = new ValidationResult { IsValid = true };
            var value = _propertySelector(entity);

            if (value < _min || value > _max)
            {
                result.AddError(_propertyName,
                    $"{_propertyName} must be between {_min} and {_max}. Current value: {value}",
                    ValidationRuleType.Range);
            }

            return result;
        }
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}