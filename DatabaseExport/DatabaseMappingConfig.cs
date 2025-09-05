using SimpleDataEngine.DatabaseExport.Converters;
using System.Reflection;

namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Database mapping configuration for entity to table mapping
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class DatabaseMappingConfig<T> where T : class
    {
        /// <summary>
        /// Property mappings for the entity
        /// </summary>
        public List<PropertyMapping> PropertyMappings { get; set; } = new();

        /// <summary>
        /// Database table name
        /// </summary>
        public string TableName { get; set; } = typeof(T).Name;

        /// <summary>
        /// Schema name (for databases that support schemas)
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Index definitions
        /// </summary>
        public List<string> Indexes { get; set; } = new();

        /// <summary>
        /// Custom SQL to execute before table creation
        /// </summary>
        public List<string> CustomSqlBefore { get; set; } = new();

        /// <summary>
        /// Custom SQL to execute after data export
        /// </summary>
        public List<string> CustomSqlAfter { get; set; } = new();

        /// <summary>
        /// Table constraints
        /// </summary>
        public List<string> TableConstraints { get; set; } = new();

        /// <summary>
        /// Foreign key relationships
        /// </summary>
        public List<ForeignKeyMapping> ForeignKeys { get; set; } = new();

        /// <summary>
        /// Whether to drop table if it exists
        /// </summary>
        public bool DropTableIfExists { get; set; } = false;

        /// <summary>
        /// Whether to create table if it doesn't exist
        /// </summary>
        public bool CreateTableIfNotExists { get; set; } = true;

        /// <summary>
        /// Batch size for bulk inserts
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Additional mapping metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Maps a property to a database column
        /// </summary>
        /// <param name="sourceProperty">Source property name</param>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="dataType">Database data type</param>
        /// <param name="converter">Optional value converter</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> MapProperty(string sourceProperty, string targetColumn, string dataType, IValueConverter converter = null)
        {
            var mapping = new PropertyMapping
            {
                SourceProperty = sourceProperty,
                TargetColumn = targetColumn,
                DataType = dataType,
                Converter = converter
            };

            PropertyMappings.Add(mapping);
            return this;
        }

        /// <summary>
        /// Maps a property with advanced options
        /// </summary>
        /// <param name="sourceProperty">Source property name</param>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="dataType">Database data type</param>
        /// <param name="options">Advanced mapping options</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> MapProperty(string sourceProperty, string targetColumn, string dataType, PropertyMappingOptions options)
        {
            var mapping = new PropertyMapping
            {
                SourceProperty = sourceProperty,
                TargetColumn = targetColumn,
                DataType = dataType,
                Converter = options.Converter,
                IsPrimaryKey = options.IsPrimaryKey,
                AllowNull = options.AllowNull,
                DefaultValue = options.DefaultValue,
                MaxLength = options.MaxLength,
                Precision = options.Precision,
                Scale = options.Scale,
                CreateIndex = options.CreateIndex,
                IndexName = options.IndexName,
                UniqueIndex = options.UniqueIndex,
                Constraints = options.Constraints?.ToList() ?? new List<string>()
            };

            PropertyMappings.Add(mapping);
            return this;
        }

        /// <summary>
        /// Excludes a property from export
        /// </summary>
        /// <param name="propertyName">Property name to exclude</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> ExcludeProperty(string propertyName)
        {
            var mapping = new PropertyMapping
            {
                SourceProperty = propertyName,
                IsExcluded = true
            };

            PropertyMappings.Add(mapping);
            return this;
        }

        /// <summary>
        /// Adds a database index
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="columns">Column names for the index</param>
        /// <param name="unique">Whether the index should be unique</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> AddIndex(string indexName, string[] columns, bool unique = false)
        {
            var uniqueKeyword = unique ? "UNIQUE " : "";
            var indexSql = $"CREATE {uniqueKeyword}INDEX {indexName} ON {GetFullTableName()} ({string.Join(", ", columns.Select(c => $"[{c}]"))})";
            Indexes.Add(indexSql);
            return this;
        }

        /// <summary>
        /// Adds a database index on a single column
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="column">Column name</param>
        /// <param name="unique">Whether the index should be unique</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> AddIndex(string indexName, string column, bool unique = false)
        {
            return AddIndex(indexName, new[] { column }, unique);
        }

        /// <summary>
        /// Adds a foreign key relationship
        /// </summary>
        /// <param name="columnName">Local column name</param>
        /// <param name="referencedTable">Referenced table name</param>
        /// <param name="referencedColumn">Referenced column name</param>
        /// <param name="onDelete">ON DELETE action</param>
        /// <param name="onUpdate">ON UPDATE action</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> AddForeignKey(string columnName, string referencedTable, string referencedColumn,
            ForeignKeyAction onDelete = ForeignKeyAction.NoAction, ForeignKeyAction onUpdate = ForeignKeyAction.NoAction)
        {
            var foreignKey = new ForeignKeyMapping
            {
                ColumnName = columnName,
                ReferencedTable = referencedTable,
                ReferencedColumn = referencedColumn,
                OnDelete = onDelete,
                OnUpdate = onUpdate
            };

            ForeignKeys.Add(foreignKey);
            return this;
        }

        /// <summary>
        /// Adds a table constraint
        /// </summary>
        /// <param name="constraint">Constraint SQL</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> AddConstraint(string constraint)
        {
            TableConstraints.Add(constraint);
            return this;
        }

        /// <summary>
        /// Sets the table schema
        /// </summary>
        /// <param name="schemaName">Schema name</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> SetSchema(string schemaName)
        {
            SchemaName = schemaName;
            return this;
        }

        /// <summary>
        /// Sets the table name
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> SetTableName(string tableName)
        {
            TableName = tableName;
            return this;
        }

        /// <summary>
        /// Configures batch settings
        /// </summary>
        /// <param name="batchSize">Batch size for inserts</param>
        /// <param name="connectionTimeout">Connection timeout in seconds</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> ConfigureBatch(int batchSize, int connectionTimeout = 30)
        {
            BatchSize = batchSize;
            ConnectionTimeoutSeconds = connectionTimeout;
            return this;
        }

        /// <summary>
        /// Auto-maps all properties using conventions
        /// </summary>
        /// <param name="conventions">Mapping conventions to use</param>
        /// <returns>This mapping config for chaining</returns>
        public DatabaseMappingConfig<T> AutoMap(MappingConventions conventions = null)
        {
            conventions ??= new MappingConventions();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !conventions.IsExcluded(p));

            foreach (var property in properties)
            {
                if (PropertyMappings.Any(m => m.SourceProperty == property.Name))
                    continue; // Skip already mapped properties

                var columnName = conventions.GetColumnName(property);
                var dataType = conventions.GetDataType(property);
                var converter = conventions.GetConverter(property);

                var mapping = new PropertyMapping
                {
                    SourceProperty = property.Name,
                    TargetColumn = columnName,
                    DataType = dataType,
                    Converter = converter,
                    IsPrimaryKey = conventions.IsPrimaryKey(property),
                    AllowNull = conventions.AllowNull(property),
                    MaxLength = conventions.GetMaxLength(property)
                };

                PropertyMappings.Add(mapping);

                // Auto-create indexes for common patterns
                if (conventions.ShouldCreateIndex(property))
                {
                    var indexName = $"IX_{TableName}_{columnName}";
                    AddIndex(indexName, columnName, conventions.ShouldCreateUniqueIndex(property));
                }
            }

            return this;
        }

        /// <summary>
        /// Validates the mapping configuration
        /// </summary>
        /// <returns>Validation result</returns>
        public MappingConfigValidationResult Validate()
        {
            var result = new MappingConfigValidationResult { IsValid = true };

            // Validate table name
            if (string.IsNullOrWhiteSpace(TableName))
            {
                result.IsValid = false;
                result.Errors.Add("Table name cannot be null or empty");
            }

            // Validate property mappings
            var duplicateColumns = PropertyMappings
                .Where(m => !m.IsExcluded)
                .GroupBy(m => m.TargetColumn)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateColumn in duplicateColumns)
            {
                result.IsValid = false;
                result.Errors.Add($"Duplicate target column: {duplicateColumn}");
            }

            // Validate individual property mappings
            foreach (var mapping in PropertyMappings.Where(m => !m.IsExcluded))
            {
                var mappingValidation = mapping.Validate();
                if (!mappingValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(mappingValidation.Errors.Select(e => $"{mapping.SourceProperty}: {e}"));
                }
                result.Warnings.AddRange(mappingValidation.Warnings.Select(w => $"{mapping.SourceProperty}: {w}"));
            }

            // Check for primary key
            var primaryKeys = PropertyMappings.Where(m => !m.IsExcluded && m.IsPrimaryKey).ToList();
            if (!primaryKeys.Any())
            {
                result.Warnings.Add("No primary key defined for table");
            }
            else if (primaryKeys.Count > 1)
            {
                result.Warnings.Add("Multiple primary keys defined - consider using composite primary key");
            }

            // Validate foreign keys
            foreach (var foreignKey in ForeignKeys)
            {
                var localColumn = PropertyMappings.FirstOrDefault(m => m.TargetColumn == foreignKey.ColumnName);
                if (localColumn == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Foreign key references non-existent column: {foreignKey.ColumnName}");
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the full table name including schema
        /// </summary>
        /// <returns>Full table name</returns>
        public string GetFullTableName()
        {
            return string.IsNullOrEmpty(SchemaName) ? $"[{TableName}]" : $"[{SchemaName}].[{TableName}]";
        }

        /// <summary>
        /// Gets all mapped columns (excluding excluded properties)
        /// </summary>
        /// <returns>List of mapped columns</returns>
        public List<PropertyMapping> GetMappedColumns()
        {
            return PropertyMappings.Where(m => !m.IsExcluded).ToList();
        }

        /// <summary>
        /// Gets the primary key mapping
        /// </summary>
        /// <returns>Primary key mapping or null</returns>
        public PropertyMapping GetPrimaryKey()
        {
            return PropertyMappings.FirstOrDefault(m => !m.IsExcluded && m.IsPrimaryKey);
        }

        /// <summary>
        /// Clones the mapping configuration
        /// </summary>
        /// <returns>Cloned configuration</returns>
        public DatabaseMappingConfig<T> Clone()
        {
            var clone = new DatabaseMappingConfig<T>
            {
                TableName = TableName,
                SchemaName = SchemaName,
                DropTableIfExists = DropTableIfExists,
                CreateTableIfNotExists = CreateTableIfNotExists,
                BatchSize = BatchSize,
                ConnectionTimeoutSeconds = ConnectionTimeoutSeconds,
                PropertyMappings = PropertyMappings.Select(ClonePropertyMapping).ToList(),
                Indexes = new List<string>(Indexes),
                CustomSqlBefore = new List<string>(CustomSqlBefore),
                CustomSqlAfter = new List<string>(CustomSqlAfter),
                TableConstraints = new List<string>(TableConstraints),
                ForeignKeys = ForeignKeys.Select(CloneForeignKey).ToList(),
                Metadata = new Dictionary<string, object>(Metadata)
            };

            return clone;
        }

        #region Private Methods

        private static PropertyMapping ClonePropertyMapping(PropertyMapping original)
        {
            return new PropertyMapping
            {
                SourceProperty = original.SourceProperty,
                TargetColumn = original.TargetColumn,
                DataType = original.DataType,
                Converter = original.Converter,
                IsPrimaryKey = original.IsPrimaryKey,
                AllowNull = original.AllowNull,
                IsExcluded = original.IsExcluded,
                DefaultValue = original.DefaultValue,
                MaxLength = original.MaxLength,
                Precision = original.Precision,
                Scale = original.Scale,
                CreateIndex = original.CreateIndex,
                IndexName = original.IndexName,
                UniqueIndex = original.UniqueIndex,
                Constraints = new List<string>(original.Constraints),
                Metadata = new Dictionary<string, object>(original.Metadata)
            };
        }

        private static ForeignKeyMapping CloneForeignKey(ForeignKeyMapping original)
        {
            return new ForeignKeyMapping
            {
                ColumnName = original.ColumnName,
                ReferencedTable = original.ReferencedTable,
                ReferencedColumn = original.ReferencedColumn,
                OnDelete = original.OnDelete,
                OnUpdate = original.OnUpdate
            };
        }

        #endregion
    }

    /// <summary>
    /// Property mapping options for advanced configuration
    /// </summary>
    public class PropertyMappingOptions
    {
        public IValueConverter Converter { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool AllowNull { get; set; } = true;
        public object DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool CreateIndex { get; set; }
        public string IndexName { get; set; }
        public bool UniqueIndex { get; set; }
        public IEnumerable<string> Constraints { get; set; }
    }

    /// <summary>
    /// Foreign key mapping
    /// </summary>
    public class ForeignKeyMapping
    {
        public string ColumnName { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
        public ForeignKeyAction OnDelete { get; set; } = ForeignKeyAction.NoAction;
        public ForeignKeyAction OnUpdate { get; set; } = ForeignKeyAction.NoAction;
    }

    /// <summary>
    /// Foreign key actions
    /// </summary>
    public enum ForeignKeyAction
    {
        NoAction,
        Cascade,
        SetNull,
        SetDefault,
        Restrict
    }

    /// <summary>
    /// Mapping configuration validation result
    /// </summary>
    public class MappingConfigValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
    }

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