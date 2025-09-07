namespace SimpleDataEngine.DatabaseExport
{
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
}