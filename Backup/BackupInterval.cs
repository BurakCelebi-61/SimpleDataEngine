namespace SimpleDataEngine.Backup
{
    /// <summary>
    /// Backup scheduling intervals
    /// </summary>
    public enum BackupInterval
    {
        Manual = 0,
        Hourly = 1,
        Daily = 2,
        Weekly = 3,
        Monthly = 4
    }
}