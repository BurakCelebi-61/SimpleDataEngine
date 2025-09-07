namespace SimpleDataEngine.DatabaseExport
{
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
}