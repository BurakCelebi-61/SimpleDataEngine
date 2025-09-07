// Security/EncryptionConfig.cs
namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Validation result for database operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Information { get; set; } = new List<string>();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ValidationDuration { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Add an error to the validation result
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Add a warning to the validation result
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Add information to the validation result
        /// </summary>
        public void AddInformation(string information)
        {
            Information.Add(information);
        }

        /// <summary>
        /// Combine with another validation result
        /// </summary>
        public void Combine(ValidationResult other)
        {
            if (other == null) return;

            if (!other.IsValid)
            {
                IsValid = false;
            }

            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            Information.AddRange(other.Information);

            foreach (var kvp in other.AdditionalData)
            {
                AdditionalData[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Get summary of validation result
        /// </summary>
        public override string ToString()
        {
            var summary = IsValid ? "Valid" : "Invalid";
            summary += $" (Errors: {Errors.Count}, Warnings: {Warnings.Count})";
            return summary;
        }
    }
}