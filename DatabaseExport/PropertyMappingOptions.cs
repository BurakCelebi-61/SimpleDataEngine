namespace SimpleDataEngine.DatabaseExport
{
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
}