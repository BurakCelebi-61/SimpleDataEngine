using System.Text;

namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Validates an entity and throws exception if invalid
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity to validate</param>
        /// <param name="options">Validation options</param>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        public static void ValidateAndThrow<T>(this T entity, ValidationOptions options = null) where T : class
        {
            var result = SimpleValidator.Validate(entity, options);
            if (!result.IsValid)
            {
                var errorMessages = result.Issues
                    .Where(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical)
                    .Select(i => $"{i.PropertyName}: {i.Message}")
                    .ToList();

                throw new ValidationException($"Validation failed:\n{string.Join("\n", errorMessages)}");
            }
        }

        /// <summary>
        /// Gets validation summary as formatted string
        /// </summary>
        /// <param name="result">Validation result</param>
        /// <returns>Formatted validation summary</returns>
        public static string ToSummaryString(this ValidationResult result)
        {
            if (result.IsValid)
                return "✓ Valid";

            var summary = new StringBuilder();
            summary.AppendLine($"❌ Invalid ({result.ErrorCount} errors, {result.WarningCount} warnings)");
            summary.AppendLine();

            if (result.Issues.Any())
            {
                var groupedIssues = result.Issues
                    .GroupBy(i => i.Severity)
                    .OrderByDescending(g => (int)g.Key);

                foreach (var group in groupedIssues)
                {
                    var icon = group.Key switch
                    {
                        ValidationSeverity.Critical => "🚨",
                        ValidationSeverity.Error => "❌",
                        ValidationSeverity.Warning => "⚠️",
                        ValidationSeverity.Info => "ℹ️",
                        _ => "•"
                    };

                    summary.AppendLine($"{icon} {group.Key}s:");
                    foreach (var issue in group.Take(10)) // Limit to first 10 per severity
                    {
                        summary.AppendLine($"  • {issue.PropertyName}: {issue.Message}");
                    }

                    if (group.Count() > 10)
                    {
                        summary.AppendLine($"  ... and {group.Count() - 10} more");
                    }

                    summary.AppendLine();
                }
            }

            return summary.ToString().TrimEnd();
        }

        /// <summary>
        /// Gets validation summary for bulk validation
        /// </summary>
        /// <param name="result">Bulk validation result</param>
        /// <returns>Formatted bulk validation summary</returns>
        public static string ToSummaryString(this BulkValidationResult result)
        {
            var summary = new StringBuilder();

            if (result.IsValid)
            {
                summary.AppendLine($"✓ All {result.TotalProcessed} entities are valid");
            }
            else
            {
                summary.AppendLine($"❌ Bulk Validation Results:");
                summary.AppendLine($"  • Total Processed: {result.TotalProcessed}");
                summary.AppendLine($"  • Valid: {result.ValidCount}");
                summary.AppendLine($"  • Invalid: {result.InvalidCount}");
                summary.AppendLine($"  • Total Errors: {result.TotalErrors}");
                summary.AppendLine($"  • Total Warnings: {result.TotalWarnings}");
                summary.AppendLine($"  • Duration: {result.Duration.TotalMilliseconds:F0}ms");
            }

            return summary.ToString().TrimEnd();
        }
    }
}