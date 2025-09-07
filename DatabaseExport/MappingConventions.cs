using SimpleDataEngine.DatabaseExport.Converters;
using System.Reflection;

namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Mapping conventions for auto-mapping
    /// </summary>
    public class MappingConventions
    {
        /// <summary>
        /// Gets the database column name for a property
        /// </summary>
        public virtual string GetColumnName(PropertyInfo property)
        {
            // Convert PascalCase to snake_case by default
            return property.Name;
        }

        /// <summary>
        /// Gets the database data type for a property
        /// </summary>
        public virtual string GetDataType(PropertyInfo property)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return type.Name switch
            {
                nameof(Int32) => "INTEGER",
                nameof(Int64) => "BIGINT",
                nameof(String) => "NVARCHAR(255)",
                nameof(DateTime) => "DATETIME2",
                nameof(Boolean) => "BIT",
                nameof(Decimal) => "DECIMAL(18,2)",
                nameof(Double) => "FLOAT",
                nameof(Guid) => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR(MAX)"
            };
        }

        /// <summary>
        /// Gets a value converter for a property
        /// </summary>
        public virtual IValueConverter GetConverter(PropertyInfo property)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return type switch
            {
                var t when t == typeof(DateTime) => new DateTimeToUnixConverter(),
                _ => null
            };
        }

        /// <summary>
        /// Checks if a property should be excluded from mapping
        /// </summary>
        public virtual bool IsExcluded(PropertyInfo property)
        {
            // Exclude properties that can't be written to database easily
            return !property.CanRead ||
                   property.PropertyType.IsClass && property.PropertyType != typeof(string) ||
                   property.PropertyType.IsInterface ||
                   property.PropertyType.IsArray;
        }

        /// <summary>
        /// Checks if a property is a primary key
        /// </summary>
        public virtual bool IsPrimaryKey(PropertyInfo property)
        {
            return property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a property allows null values
        /// </summary>
        public virtual bool AllowNull(PropertyInfo property)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) != null ||
                   !property.PropertyType.IsValueType;
        }

        /// <summary>
        /// Gets the maximum length for string properties
        /// </summary>
        public virtual int? GetMaxLength(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
            {
                // Check for StringLength attribute or use default
                var stringLengthAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>();
                return stringLengthAttr?.MaximumLength ?? 255;
            }
            return null;
        }

        /// <summary>
        /// Checks if an index should be created for a property
        /// </summary>
        public virtual bool ShouldCreateIndex(PropertyInfo property)
        {
            // Create indexes for common patterns
            var name = property.Name.ToLower();
            return name.Contains("email") ||
                   name.Contains("username") ||
                   name.EndsWith("id") ||
                   name.Contains("code");
        }

        /// <summary>
        /// Checks if a unique index should be created for a property
        /// </summary>
        public virtual bool ShouldCreateUniqueIndex(PropertyInfo property)
        {
            var name = property.Name.ToLower();
            return name.Contains("email") || name.Contains("username");
        }
    }
}