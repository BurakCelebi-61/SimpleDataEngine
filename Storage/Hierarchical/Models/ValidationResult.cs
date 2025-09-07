namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Validation result helper class
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
                Errors.Add(error);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors ?? Enumerable.Empty<string>())
                AddError(error);
        }

        public override string ToString()
        {
            return IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
        }
    }
}