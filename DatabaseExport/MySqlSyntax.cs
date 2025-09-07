namespace SimpleDataEngine.DatabaseExport
{
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
}