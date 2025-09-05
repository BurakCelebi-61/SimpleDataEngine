namespace SimpleDataEngine.Backup
{
    /// <summary>
    /// Backup retention policies
    /// </summary>
    public enum BackupRetentionPolicy
    {
        KeepAll = 0,
        KeepLast = 1,
        KeepByAge = 2,
        KeepBySize = 3,
        Smart = 4  // Keep daily for 7 days, weekly for 4 weeks, monthly for 12 months
    }
}