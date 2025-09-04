namespace SimpleDataEngine.Validation
{
    /// <summary>
    /// Validation rule types
    /// </summary>
    public enum ValidationRuleType
    {
        Required,
        Range,
        StringLength,
        RegularExpression,
        Custom,
        BusinessRule,
        DataIntegrity,
        ReferentialIntegrity
    }
    /// <summary>
    /// Validation extensions for easier usage
    /// </summary>
}