namespace SimpleDataEngine.DatabaseExport
{
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
}