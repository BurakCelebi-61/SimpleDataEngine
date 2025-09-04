namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Custom validation exception
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationResult ValidationResult { get; }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, ValidationResult validationResult) : base(message)
        {
            ValidationResult = validationResult;
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}