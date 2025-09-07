using System.Text;

namespace SimpleDataEngine.DatabaseExport
{
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
                if (i < columns.Count - 1 || constraints?.Any() == true)
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
}