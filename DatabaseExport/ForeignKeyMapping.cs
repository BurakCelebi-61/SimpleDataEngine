namespace SimpleDataEngine.DatabaseExport
{
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
}