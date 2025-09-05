using System.Text;

namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Base class for database-specific SQL syntax
    /// </summary>
    public abstract class SqlSyntax
    {
        /// <summary>
        /// Database type this syntax supports
        /// </summary>
        public abstract DatabaseType DatabaseType { get; }

        /// <summary>
        /// Character used to quote identifiers
        /// </summary>
        public abstract string IdentifierQuote { get; }

        /// <summary>
        /// Character used to quote string literals
        /// </summary>
        public abstract string StringQuote { get; }

        /// <summary>
        /// SQL statement terminator
        /// </summary>
        public virtual string StatementTerminator => ";";

        /// <summary>
        /// Maximum identifier length
        /// </summary>
        public virtual int MaxIdentifierLength => 128;

        /// <summary>
        /// Whether the database supports schemas
        /// </summary>
        public virtual bool SupportsSchemas => true;

        /// <summary>
        /// Whether the database supports foreign keys
        /// </summary>
        public virtual bool SupportsForeignKeys => true;

        /// <summary>
        /// Whether the database supports check constraints
        /// </summary>
        public virtual bool SupportsCheckConstraints => true;

        /// <summary>
        /// Whether the database supports auto-increment columns
        /// </summary>
        public virtual bool SupportsAutoIncrement => true;

        /// <summary>
        /// Quotes an identifier (table name, column name, etc.)
        /// </summary>
        /// <param name="identifier">Identifier to quote</param>
        /// <returns>Quoted identifier</returns>
        public virtual string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            return $"{IdentifierQuote}{identifier.Replace(IdentifierQuote, IdentifierQuote + IdentifierQuote)}{IdentifierQuote}";
        }

        /// <summary>
        /// Quotes a string literal
        /// </summary>
        /// <param name="value">String value to quote</param>
        /// <returns>Quoted string literal</returns>
        public virtual string QuoteString(string value)
        {
            if (value == null)
                return "NULL";

            return $"{StringQuote}{value.Replace(StringQuote, StringQuote + StringQuote)}{StringQuote}";
        }

        /// <summary>
        /// Formats a value for SQL
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <returns>Formatted SQL value</returns>
        public virtual string FormatValue(object value)
        {
            return value switch
            {
                null => "NULL",
                string s => QuoteString(s),
                bool b => FormatBoolean(b),
                DateTime dt => FormatDateTime(dt),
                DateTimeOffset dto => FormatDateTime(dto.DateTime),
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Guid g => FormatGuid(g),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Formats a boolean value
        /// </summary>
        /// <param name="value">Boolean value</param>
        /// <returns>Formatted boolean</returns>
        public virtual string FormatBoolean(bool value)
        {
            return value ? "1" : "0";
        }

        /// <summary>
        /// Formats a DateTime value
        /// </summary>
        /// <param name="value">DateTime value</param>
        /// <returns>Formatted DateTime</returns>
        public virtual string FormatDateTime(DateTime value)
        {
            return QuoteString(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Formats a Guid value
        /// </summary>
        /// <param name="value">Guid value</param>
        /// <returns>Formatted Guid</returns>
        public virtual string FormatGuid(Guid value)
        {
            return QuoteString(value.ToString());
        }

        /// <summary>
        /// Gets the CREATE TABLE statement
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columns">Column definitions</param>
        /// <param name="constraints">Table constraints</param>
        /// <param name="ifNotExists">Whether to add IF NOT EXISTS clause</param>
        /// <returns>CREATE TABLE SQL</returns>
        public virtual string GetCreateTableStatement(string tableName, List<string> columns, List<string> constraints = null, bool ifNotExists = true)
        {
            var sql = new StringBuilder();

            sql.Append("CREATE TABLE ");
            if (ifNotExists && SupportsIfNotExists)
            {
                sql.Append("IF NOT EXISTS ");
            }
            sql.AppendLine(QuoteIdentifier(tableName));
            sql.AppendLine("(");

            // Add columns
            for (int i = 0; i < columns.Count; i++)
            {
                sql.Append("    ");
                sql.Append(columns[i]);
                if (i < columns.Count - 1 || (constraints?.Any() == true))
                {
                    sql.Append(",");
                }
                sql.AppendLine();
            }

            // Add constraints
            if (constraints?.Any() == true)
            {
                for (int i = 0; i < constraints.Count; i++)
                {
                    sql.Append("    ");
                    sql.Append(constraints[i]);
                    if (i < constraints.Count - 1)
                    {
                        sql.Append(",");
                    }
                    sql.AppendLine();
                }
            }

            sql.Append(")");
            return sql.ToString();
        }

        /// <summary>
        /// Gets the DROP TABLE statement
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="ifExists">Whether to add IF EXISTS clause</param>
        /// <returns>DROP TABLE SQL</returns>
        public virtual string GetDropTableStatement(string tableName, bool ifExists = true)
        {
            var sql = new StringBuilder();
            sql.Append("DROP TABLE ");
            if (ifExists && SupportsIfExists)
            {
                sql.Append("IF EXISTS ");
            }
            sql.Append(QuoteIdentifier(tableName));
            return sql.ToString();
        }

        /// <summary>
        /// Gets the INSERT statement
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columns">Column names</param>
        /// <param name="values">Values to insert</param>
        /// <returns>INSERT SQL</returns>
        public virtual string GetInsertStatement(string tableName, List<string> columns, List<object> values)
        {
            var quotedColumns = columns.Select(QuoteIdentifier);
            var formattedValues = values.Select(FormatValue);

            return $"INSERT INTO {QuoteIdentifier(tableName)} ({string.Join(", ", quotedColumns)}) VALUES ({string.Join(", ", formattedValues)})";
        }

        /// <summary>
        /// Gets the bulk INSERT statement
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columns">Column names</param>
        /// <param name="valueRows">Rows of values to insert</param>
        /// <returns>Bulk INSERT SQL</returns>
        public virtual string GetBulkInsertStatement(string tableName, List<string> columns, List<List<object>> valueRows)
        {
            if (!valueRows.Any())
                return string.Empty;

            var quotedColumns = columns.Select(QuoteIdentifier);
            var sql = new StringBuilder();

            sql.AppendLine($"INSERT INTO {QuoteIdentifier(tableName)} ({string.Join(", ", quotedColumns)}) VALUES");

            for (int i = 0; i < valueRows.Count; i++)
            {
                var formattedValues = valueRows[i].Select(FormatValue);
                sql.Append($"({string.Join(", ", formattedValues)})");

                if (i < valueRows.Count - 1)
                {
                    sql.AppendLine(",");
                }
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the CREATE INDEX statement
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="tableName">Table name</param>
        /// <param name="columns">Column names</param>
        /// <param name="unique">Whether index is unique</param>
        /// <returns>CREATE INDEX SQL</returns>
        public virtual string GetCreateIndexStatement(string indexName, string tableName, List<string> columns, bool unique = false)
        {
            var uniqueKeyword = unique ? "UNIQUE " : "";
            var quotedColumns = columns.Select(QuoteIdentifier);

            return $"CREATE {uniqueKeyword}INDEX {QuoteIdentifier(indexName)} ON {QuoteIdentifier(tableName)} ({string.Join(", ", quotedColumns)})";
        }

        /// <summary>
        /// Gets the ADD FOREIGN KEY statement
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnName">Column name</param>
        /// <param name="referencedTable">Referenced table</param>
        /// <param name="referencedColumn">Referenced column</param>
        /// <param name="onDelete">ON DELETE action</param>
        /// <param name="onUpdate">ON UPDATE action</param>
        /// <returns>ADD FOREIGN KEY SQL</returns>
        public virtual string GetAddForeignKeyStatement(string tableName, string columnName, string referencedTable,
            string referencedColumn, ForeignKeyAction onDelete = ForeignKeyAction.NoAction, ForeignKeyAction onUpdate = ForeignKeyAction.NoAction)
        {
            if (!SupportsForeignKeys)
                return string.Empty;

            var sql = new StringBuilder();
            sql.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ADD CONSTRAINT ");
            sql.Append($"FK_{tableName}_{columnName} ");
            sql.Append($"FOREIGN KEY ({QuoteIdentifier(columnName)}) ");
            sql.Append($"REFERENCES {QuoteIdentifier(referencedTable)} ({QuoteIdentifier(referencedColumn)})");

            if (onDelete != ForeignKeyAction.NoAction)
            {
                sql.Append($" ON DELETE {GetForeignKeyActionSql(onDelete)}");
            }

            if (onUpdate != ForeignKeyAction.NoAction)
            {
                sql.Append($" ON UPDATE {GetForeignKeyActionSql(onUpdate)}");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the SQL for foreign key actions
        /// </summary>
        /// <param name="action">Foreign key action</param>
        /// <returns>SQL for the action</returns>
        protected virtual string GetForeignKeyActionSql(ForeignKeyAction action)
        {
            return action switch
            {
                ForeignKeyAction.Cascade => "CASCADE",
                ForeignKeyAction.SetNull => "SET NULL",
                ForeignKeyAction.SetDefault => "SET DEFAULT",
                ForeignKeyAction.Restrict => "RESTRICT",
                _ => "NO ACTION"
            };
        }

        /// <summary>
        /// Whether database supports IF NOT EXISTS clause
        /// </summary>
        public virtual bool SupportsIfNotExists => true;

        /// <summary>
        /// Whether database supports IF EXISTS clause
        /// </summary>
        public virtual bool SupportsIfExists => true;

        /// <summary>
        /// Gets the data type mapping for .NET types
        /// </summary>
        /// <param name="dotNetType">.NET type</param>
        /// <returns>Database data type</returns>
        public virtual string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "SMALLINT",
                nameof(Int32) => "INTEGER",
                nameof(Int64) => "BIGINT",
                nameof(String) => "NVARCHAR(255)",
                nameof(DateTime) => "DATETIME",
                nameof(Boolean) => "BIT",
                nameof(Decimal) => "DECIMAL(18,2)",
                nameof(Double) => "FLOAT",
                nameof(Single) => "REAL",
                nameof(Guid) => "UNIQUEIDENTIFIER",
                nameof(Byte) => "TINYINT",
                _ => "NVARCHAR(MAX)"
            };
        }

        /// <summary>
        /// Escapes special characters in identifiers
        /// </summary>
        /// <param name="identifier">Identifier to escape</param>
        /// <returns>Escaped identifier</returns>
        public virtual string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            // Remove invalid characters and ensure it starts with letter or underscore
            var escaped = new StringBuilder();
            for (int i = 0; i < identifier.Length; i++)
            {
                var c = identifier[i];
                if (i == 0)
                {
                    if (char.IsLetter(c) || c == '_')
                        escaped.Append(c);
                    else
                        escaped.Append('_');
                }
                else
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                        escaped.Append(c);
                    else
                        escaped.Append('_');
                }
            }

            var result = escaped.ToString();

            // Truncate if too long
            if (result.Length > MaxIdentifierLength)
            {
                result = result.Substring(0, MaxIdentifierLength);
            }

            return result;
        }
    }

    /// <summary>
    /// SQLite-specific SQL syntax
    /// </summary>
    public class SqliteSyntax : SqlSyntax
    {
        public override DatabaseType DatabaseType => DatabaseType.SQLite;
        public override string IdentifierQuote => "\"";
        public override string StringQuote => "'";
        public override bool SupportsSchemas => false;
        public override int MaxIdentifierLength => 64;

        public override string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "INTEGER",
                nameof(Int32) => "INTEGER",
                nameof(Int64) => "INTEGER",
                nameof(String) => "TEXT",
                nameof(DateTime) => "TEXT",
                nameof(Boolean) => "INTEGER",
                nameof(Decimal) => "REAL",
                nameof(Double) => "REAL",
                nameof(Single) => "REAL",
                nameof(Guid) => "TEXT",
                nameof(Byte) => "INTEGER",
                _ => "TEXT"
            };
        }

        public override string FormatBoolean(bool value)
        {
            return value ? "1" : "0";
        }

        public override string FormatDateTime(DateTime value)
        {
            return QuoteString(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    /// <summary>
    /// SQL Server-specific SQL syntax
    /// </summary>
    public class SqlServerSyntax : SqlSyntax
    {
        public override DatabaseType DatabaseType => DatabaseType.SQLServer;
        public override string IdentifierQuote => "[";
        public override string StringQuote => "'";
        public override bool SupportsIfNotExists => false;
        public override int MaxIdentifierLength => 128;

        public override string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            return $"[{identifier.Replace("]", "]]")}]";
        }

        public override string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "SMALLINT",
                nameof(Int32) => "INT",
                nameof(Int64) => "BIGINT",
                nameof(String) => "NVARCHAR(255)",
                nameof(DateTime) => "DATETIME2",
                nameof(Boolean) => "BIT",
                nameof(Decimal) => "DECIMAL(18,2)",
                nameof(Double) => "FLOAT",
                nameof(Single) => "REAL",
                nameof(Guid) => "UNIQUEIDENTIFIER",
                nameof(Byte) => "TINYINT",
                _ => "NVARCHAR(MAX)"
            };
        }

        public override string GetCreateTableStatement(string tableName, List<string> columns, List<string> constraints = null, bool ifNotExists = true)
        {
            // SQL Server doesn't support IF NOT EXISTS, so we use a different approach
            var sql = new StringBuilder();

            if (ifNotExists)
            {
                sql.AppendLine($"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')");
                sql.AppendLine("BEGIN");
            }

            sql.Append("CREATE TABLE ");
            sql.AppendLine(QuoteIdentifier(tableName));
            sql.AppendLine("(");

            // Add columns
            for (int i = 0; i < columns.Count; i++)
            {
                sql.Append("    ");
                sql.Append(columns[i]);
                if (i < columns.Count - 1 || (constraints?.Any() == true))
                {
                    sql.Append(",");
                }
                sql.AppendLine();
            }

            // Add constraints
            if (constraints?.Any() == true)
            {
                for (int i = 0; i < constraints.Count; i++)
                {
                    sql.Append("    ");
                    sql.Append(constraints[i]);
                    if (i < constraints.Count - 1)
                    {
                        sql.Append(",");
                    }
                    sql.AppendLine();
                }
            }

            sql.Append(")");

            if (ifNotExists)
            {
                sql.AppendLine();
                sql.AppendLine("END");
            }

            return sql.ToString();
        }

        public override string FormatDateTime(DateTime value)
        {
            return QuoteString(value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }
    }

    /// <summary>
    /// MySQL-specific SQL syntax
    /// </summary>
    public class MySqlSyntax : SqlSyntax
    {
        public override DatabaseType DatabaseType => DatabaseType.MySQL;
        public override string IdentifierQuote => "`";
        public override string StringQuote => "'";
        public override int MaxIdentifierLength => 64;

        public override string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "SMALLINT",
                nameof(Int32) => "INT",
                nameof(Int64) => "BIGINT",
                nameof(String) => "VARCHAR(255)",
                nameof(DateTime) => "DATETIME",
                nameof(Boolean) => "BOOLEAN",
                nameof(Decimal) => "DECIMAL(18,2)",
                nameof(Double) => "DOUBLE",
                nameof(Single) => "FLOAT",
                nameof(Guid) => "CHAR(36)",
                nameof(Byte) => "TINYINT",
                _ => "TEXT"
            };
        }

        public override string FormatBoolean(bool value)
        {
            return value ? "TRUE" : "FALSE";
        }
    }

    /// <summary>
    /// PostgreSQL-specific SQL syntax
    /// </summary>
    public class PostgreSqlSyntax : SqlSyntax
    {
        public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
        public override string IdentifierQuote => "\"";
        public override string StringQuote => "'";
        public override int MaxIdentifierLength => 63;

        public override string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "SMALLINT",
                nameof(Int32) => "INTEGER",
                nameof(Int64) => "BIGINT",
                nameof(String) => "VARCHAR(255)",
                nameof(DateTime) => "TIMESTAMP",
                nameof(Boolean) => "BOOLEAN",
                nameof(Decimal) => "DECIMAL(18,2)",
                nameof(Double) => "DOUBLE PRECISION",
                nameof(Single) => "REAL",
                nameof(Guid) => "UUID",
                nameof(Byte) => "SMALLINT",
                _ => "TEXT"
            };
        }

        public override string FormatBoolean(bool value)
        {
            return value ? "TRUE" : "FALSE";
        }

        public override string FormatGuid(Guid value)
        {
            return QuoteString(value.ToString());
        }
    }

    /// <summary>
    /// Oracle-specific SQL syntax
    /// </summary>
    public class OracleSyntax : SqlSyntax
    {
        public override DatabaseType DatabaseType => DatabaseType.Oracle;
        public override string IdentifierQuote => "\"";
        public override string StringQuote => "'";
        public override int MaxIdentifierLength => 30;

        public override string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "NUMBER(5)",
                nameof(Int32) => "NUMBER(10)",
                nameof(Int64) => "NUMBER(19)",
                nameof(String) => "VARCHAR2(255)",
                nameof(DateTime) => "DATE",
                nameof(Boolean) => "NUMBER(1)",
                nameof(Decimal) => "NUMBER(18,2)",
                nameof(Double) => "BINARY_DOUBLE",
                nameof(Single) => "BINARY_FLOAT",
                nameof(Guid) => "RAW(16)",
                nameof(Byte) => "NUMBER(3)",
                _ => "CLOB"
            };
        }

        public override string FormatBoolean(bool value)
        {
            return value ? "1" : "0";
        }

        public override string FormatDateTime(DateTime value)
        {
            return $"TO_DATE('{value:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
        }
    }

    /// <summary>
    /// Access-specific SQL syntax
    /// </summary>
    public class AccessSyntax : SqlSyntax
    {
        public override DatabaseType DatabaseType => DatabaseType.Access;
        public override string IdentifierQuote => "[";
        public override string StringQuote => "'";
        public override bool SupportsSchemas => false;
        public override bool SupportsForeignKeys => true;
        public override bool SupportsCheckConstraints => false;
        public override int MaxIdentifierLength => 64;

        public override string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            return $"[{identifier.Replace("]", "]]")}]";
        }

        public override string GetDataType(Type dotNetType)
        {
            var type = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

            return type.Name switch
            {
                nameof(Int16) => "Short",
                nameof(Int32) => "Long",
                nameof(Int64) => "Long",
                nameof(String) => "Text(255)",
                nameof(DateTime) => "DateTime",
                nameof(Boolean) => "YesNo",
                nameof(Decimal) => "Currency",
                nameof(Double) => "Double",
                nameof(Single) => "Single",
                nameof(Guid) => "Text(38)",
                nameof(Byte) => "Byte",
                _ => "Memo"
            };
        }

        public override string FormatBoolean(bool value)
        {
            return value ? "True" : "False";
        }

        public override string FormatDateTime(DateTime value)
        {
            return $"#{value:MM/dd/yyyy HH:mm:ss}#";
        }
    }
}