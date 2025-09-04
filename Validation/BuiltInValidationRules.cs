namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Built-in validation rules
    /// </summary>
    public static class BuiltInValidationRules
    {
        /// <summary>
        /// Creates a rule that validates string is not null or empty
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="propertySelector">Property selector</param>
        /// <param name="propertyName">Property name for error messages</param>
        /// <returns>Validation rule</returns>
        public static IValidationRule<T> NotNullOrEmpty<T>(Func<T, string> propertySelector, string propertyName)
        {
            return new NotNullOrEmptyRule<T>(propertySelector, propertyName);
        }

        /// <summary>
        /// Creates a rule that validates numeric range
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="propertySelector">Property selector</param>
        /// <param name="propertyName">Property name for error messages</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Validation rule</returns>
        public static IValidationRule<T> Range<T>(Func<T, decimal> propertySelector, string propertyName, decimal min, decimal max)
        {
            return new RangeRule<T>(propertySelector, propertyName, min, max);
        }

        /// <summary>
        /// Creates a rule that validates string length
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="propertySelector">Property selector</param>
        /// <param name="propertyName">Property name for error messages</param>
        /// <param name="minLength">Minimum length</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>Validation rule</returns>
        public static IValidationRule<T> StringLength<T>(Func<T, string> propertySelector, string propertyName, int minLength, int maxLength)
        {
            return new StringLengthRule<T>(propertySelector, propertyName, minLength, maxLength);
        }

        /// <summary>
        /// Creates a custom validation rule
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="ruleName">Rule name</param>
        /// <param name="validator">Validation function</param>
        /// <param name="severity">Default severity</param>
        /// <returns>Validation rule</returns>
        public static IValidationRule<T> Custom<T>(string ruleName, Func<T, ValidationOptions, ValidationResult> validator, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return new CustomValidationRule<T>(ruleName, validator, severity);
        }
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}