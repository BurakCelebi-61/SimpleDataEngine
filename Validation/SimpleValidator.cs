using SimpleDataEngine.Audit;
using SimpleDataEngine.Core;
using System.ComponentModel.DataAnnotations;

namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Simple validation engine for SimpleDataEngine
    /// </summary>
    public static class SimpleValidator
    {
        private static readonly Dictionary<Type, List<object>> _customRules = new();
        private static readonly object _lock = new object();

        /// <summary>
        /// Validates a single entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity to validate</param>
        /// <param name="options">Validation options</param>
        /// <returns>Validation result</returns>
        public static ValidationResult Validate<T>(T entity, ValidationOptions options = null) where T : class
        {
            options ??= new ValidationOptions();
            var result = new ValidationResult { IsValid = true };

            try
            {
                if (entity == null)
                {
                    result.AddError("Entity", "Entity cannot be null", ValidationRuleType.Required);
                    return result;
                }

                // Data Annotations validation
                if (options.ValidateDataAnnotations)
                {
                    ValidateDataAnnotations(entity, result, options);
                    if (options.StopOnFirstError && result.HasErrors)
                        return result;
                }

                // Custom validation rules
                if (options.ValidateBusinessRules)
                {
                    ValidateCustomRules(entity, result, options);
                    if (options.StopOnFirstError && result.HasErrors)
                        return result;
                }

                // IValidatableObject support
                if (entity is IValidatableObject validatableObject)
                {
                    var validationContext = new ValidationContext(entity);
                    var validationResults = validatableObject.Validate(validationContext);

                    foreach (var validationResult in validationResults)
                    {
                        var propertyName = validationResult.MemberNames?.FirstOrDefault() ?? "Entity";
                        result.AddError(propertyName, validationResult.ErrorMessage, ValidationRuleType.BusinessRule);
                    }
                }

                // Referential integrity (if entity implements IEntity)
                if (options.ValidateReferentialIntegrity && entity is IEntity)
                {
                    ValidateReferentialIntegrity(entity, result, options);
                }

                // Check if we have too many issues
                if (result.Issues.Count > options.MaxIssues)
                {
                    result.Issues = result.Issues.Take(options.MaxIssues).ToList();
                    result.AddWarning("Validation", $"Validation stopped after {options.MaxIssues} issues", ValidationRuleType.Custom);
                }

                // Final validation status
                result.IsValid = !result.HasErrors;

                // Log validation if there are issues
                if (result.Issues.Any())
                {
                    AuditLogger.Log("ENTITY_VALIDATION", new
                    {
                        EntityType = typeof(T).Name,
                        result.IsValid,
                        result.ErrorCount,
                        result.WarningCount,
                        Issues = result.Issues.Take(5).Select(i => new { i.PropertyName, i.Message, i.Severity })
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                result.AddError("Validation", $"Validation failed: {ex.Message}", ValidationRuleType.Custom);
                AuditLogger.LogError("VALIDATION_ERROR", ex);
                return result;
            }
        }

        /// <summary>
        /// Validates multiple entities
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entities">Entities to validate</param>
        /// <param name="options">Validation options</param>
        /// <returns>Bulk validation result</returns>
        public static BulkValidationResult ValidateBulk<T>(IEnumerable<T> entities, ValidationOptions options = null)
            where T : class
        {
            var startTime = DateTime.Now;
            var result = new BulkValidationResult();
            options ??= new ValidationOptions();

            try
            {
                var entityList = entities?.ToList() ?? new List<T>();
                result.TotalProcessed = entityList.Count;

                foreach (var entity in entityList)
                {
                    var entityResult = Validate(entity, options);
                    result.Results[entity] = entityResult;

                    if (entityResult.IsValid)
                        result.ValidCount++;
                    else
                        result.InvalidCount++;

                    if (options.StopOnFirstError && entityResult.HasErrors)
                        break;
                }

                result.IsValid = result.InvalidCount == 0;
                result.Duration = DateTime.Now - startTime;

                AuditLogger.Log("BULK_VALIDATION", new
                {
                    EntityType = typeof(T).Name,
                    result.TotalProcessed,
                    result.ValidCount,
                    result.InvalidCount,
                    result.Duration
                });

                return result;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("BULK_VALIDATION_ERROR", ex);
                result.IsValid = false;
                result.Duration = DateTime.Now - startTime;
                return result;
            }
        }

        /// <summary>
        /// Registers a custom validation rule
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="rule">Validation rule</param>
        public static void RegisterRule<T>(IValidationRule<T> rule)
        {
            lock (_lock)
            {
                var entityType = typeof(T);
                if (!_customRules.ContainsKey(entityType))
                {
                    _customRules[entityType] = new List<object>();
                }
                _customRules[entityType].Add(rule);
            }
        }

        /// <summary>
        /// Removes a custom validation rule
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="ruleName">Rule name to remove</param>
        public static void RemoveRule<T>(string ruleName)
        {
            lock (_lock)
            {
                var entityType = typeof(T);
                if (_customRules.ContainsKey(entityType))
                {
                    _customRules[entityType].RemoveAll(r =>
                        r is IValidationRule<T> rule && rule.RuleName == ruleName);
                }
            }
        }

        /// <summary>
        /// Gets all registered rules for an entity type
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>List of validation rules</returns>
        public static List<IValidationRule<T>> GetRules<T>()
        {
            lock (_lock)
            {
                var entityType = typeof(T);
                if (_customRules.ContainsKey(entityType))
                {
                    return _customRules[entityType].Cast<IValidationRule<T>>().ToList();
                }
                return new List<IValidationRule<T>>();
            }
        }

        /// <summary>
        /// Clears all custom validation rules
        /// </summary>
        public static void ClearAllRules()
        {
            lock (_lock)
            {
                _customRules.Clear();
            }
        }

        /// <summary>
        /// Quick validation check - returns only boolean result
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid<T>(T entity) where T : class
        {
            var result = Validate(entity, new ValidationOptions { StopOnFirstError = true });
            return result.IsValid;
        }

        #region Private Methods

        private static void ValidateDataAnnotations<T>(T entity, ValidationResult result, ValidationOptions options)
        {
            var validationContext = new ValidationContext(entity);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            bool isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);

            foreach (var validationResult in validationResults)
            {
                var propertyName = validationResult.MemberNames?.FirstOrDefault() ?? "Entity";
                var ruleType = GetValidationRuleType(validationResult);

                result.AddError(propertyName, validationResult.ErrorMessage, ruleType);

                if (result.Issues.Count >= options.MaxIssues)
                    break;
            }
        }

        private static void ValidateCustomRules<T>(T entity, ValidationResult result, ValidationOptions options)
        {
            var entityType = typeof(T);
            if (!_customRules.ContainsKey(entityType))
                return;

            var rules = _customRules[entityType].Cast<IValidationRule<T>>();

            foreach (var rule in rules)
            {
                try
                {
                    var ruleResult = rule.Validate(entity, options);

                    // Merge results
                    foreach (var issue in ruleResult.Issues)
                    {
                        issue.RuleName = rule.RuleName;
                        result.Issues.Add(issue);

                        if (issue.Severity == ValidationSeverity.Error ||
                            issue.Severity == ValidationSeverity.Critical)
                        {
                            result.IsValid = false;
                        }

                        if (result.Issues.Count >= options.MaxIssues)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    result.AddError("CustomRule", $"Custom rule '{rule.RuleName}' failed: {ex.Message}", ValidationRuleType.Custom);
                }
            }
        }

        // SimpleValidator.cs satır 300 civarındaki GUID karşılaştırması düzeltmesi:

        private static void ValidateReferentialIntegrity<T>(T entity, ValidationResult result, ValidationOptions options)
        {
            // This would integrate with your storage layer to check references
            // For now, just adding a placeholder implementation
            if (entity is IEntity entityWithId)
            {
                // HATALI KOD:
                // if (entityWithId.Id == Guid.Empty)

                // DÜZELTİLMİŞ KOD - IEntity.Id bir int olduğu için:
                if (entityWithId.Id <= 0)
                {
                    result.AddError("Id", "Entity ID must be greater than 0", ValidationRuleType.ReferentialIntegrity);
                }

                // UpdateTime kontrolü de eklenebilir
                if (entityWithId.UpdateTime == default(DateTime))
                {
                    result.AddError("UpdateTime", "Entity UpdateTime cannot be default", ValidationRuleType.DataIntegrity);
                }

                // UpdateTime gelecek tarih olamaz
                if (entityWithId.UpdateTime > DateTime.Now.AddMinutes(5)) // 5 dakika tolerans
                {
                    result.AddError("UpdateTime", "Entity UpdateTime cannot be in the future", ValidationRuleType.DataIntegrity);
                }
            }
        }

        private static ValidationRuleType GetValidationRuleType(System.ComponentModel.DataAnnotations.ValidationResult validationResult)
        {
            // Try to determine rule type from error message or validation attribute
            var errorMessage = validationResult.ErrorMessage?.ToLower() ?? "";

            if (errorMessage.Contains("required"))
                return ValidationRuleType.Required;
            if (errorMessage.Contains("range") || errorMessage.Contains("between"))
                return ValidationRuleType.Range;
            if (errorMessage.Contains("length") || errorMessage.Contains("characters"))
                return ValidationRuleType.StringLength;
            if (errorMessage.Contains("format") || errorMessage.Contains("pattern"))
                return ValidationRuleType.RegularExpression;

            return ValidationRuleType.Custom;
        }

        #endregion
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}