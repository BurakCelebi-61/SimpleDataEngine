namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Maps a property to a database column
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// Source property name
        /// </summary>
        public string SourceProperty { get; set; }

        /// <summary>
        /// Target database column name
        /// </summary>
        public string TargetColumn { get; set; }

        /// <summary>
        /// Database column data type
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Value converter for transformation
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Whether this column is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Whether this column allows null values
        /// </summary>
        public bool AllowNull { get; set; } = true;

        /// <summary>
        /// Whether this property should be excluded from export
        /// </summary>
        public bool IsExcluded { get; set; }

        /// <summary>
        /// Default value for the column
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Column maximum length (for string types)
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Precision for decimal types
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Scale for decimal types
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Whether this column should have an index
        /// </summary>
        public bool CreateIndex { get; set; }

        /// <summary>
        /// Index name if CreateIndex is true
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// Whether the index should be unique
        /// </summary>
        public bool UniqueIndex { get; set; }

        /// <summary>
        /// Custom column constraints
        /// </summary>
        public List<string> Constraints { get; set; } = new();

        /// <summary>
        /// Additional metadata for the mapping
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets the full column definition SQL
        /// </summary>
        /// <param name="syntax">SQL syntax helper</param>
        /// <returns>Column definition SQL</returns>
        public string GetColumnDefinition(SqlSyntax syntax)
        {
            var definition = $"{syntax.QuoteIdentifier(TargetColumn)} {DataType}";

            // Add length for string types
            if (MaxLength.HasValue && (DataType.ToUpper().Contains("VARCHAR") || DataType.ToUpper().Contains("CHAR")))
            {
                definition += $"({MaxLength.Value})";
            }

            // Add precision and scale for decimal types
            if (Precision.HasValue && DataType.ToUpper().Contains("DECIMAL"))
            {
                if (Scale.HasValue)
                    definition += $"({Precision.Value},{Scale.Value})";
                else
                    definition += $"({Precision.Value})";
            }

            // Add null constraint
            if (!AllowNull)
            {
                definition += " NOT NULL";
            }

            // Add primary key constraint
            if (IsPrimaryKey)
            {
                definition += " PRIMARY KEY";
            }

            // Add default value
            if (DefaultValue != null)
            {
                definition += $" DEFAULT {syntax.FormatValue(DefaultValue)}";
            }

            // Add custom constraints
            foreach (var constraint in Constraints)
            {
                definition += $" {constraint}";
            }

            return definition;
        }

        /// <summary>
        /// Validates the property mapping
        /// </summary>
        /// <returns>Validation result</returns>
        public PropertyMappingValidationResult Validate()
        {
            var result = new PropertyMappingValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(SourceProperty))
            {
                result.IsValid = false;
                result.Errors.Add("SourceProperty cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(TargetColumn))
            {
                result.IsValid = false;
                result.Errors.Add("TargetColumn cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(DataType))
            {
                result.IsValid = false;
                result.Errors.Add("DataType cannot be null or empty");
            }

            if (IsPrimaryKey && AllowNull)
            {
                result.Warnings.Add("Primary key columns should not allow null values");
            }

            if (MaxLength.HasValue && MaxLength.Value <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("MaxLength must be greater than 0");
            }

            if (Precision.HasValue && Precision.Value <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Precision must be greater than 0");
            }

            if (Scale.HasValue && Scale.Value < 0)
            {
                result.IsValid = false;
                result.Errors.Add("Scale cannot be negative");
            }

            if (Precision.HasValue && Scale.HasValue && Scale.Value > Precision.Value)
            {
                result.IsValid = false;
                result.Errors.Add("Scale cannot be greater than precision");
            }

            return result;
        }
    }
}