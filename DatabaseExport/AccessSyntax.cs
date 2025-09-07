namespace SimpleDataEngine.DatabaseExport
{
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