namespace SimpleDataEngine.Configuration
{
    /// <summary>
    /// Validation configuration settings
    /// </summary>
    public class ValidationConfig
    {
        public bool EnableValidation { get; set; } = true;
        public bool StrictMode { get; set; } = false;
        public bool LogValidationErrors { get; set; } = true;
    }
}