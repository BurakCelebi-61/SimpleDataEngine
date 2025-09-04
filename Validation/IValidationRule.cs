namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Interface for custom validation rules
    /// </summary>
    public interface IValidationRule<T>
    {
        string RuleName { get; }
        ValidationSeverity DefaultSeverity { get; }
        ValidationResult Validate(T entity, ValidationOptions options);
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}