namespace SimpleDataEngine.DatabaseExport
{
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
}