namespace SimpleDataEngine.Health
{
    /// <summary>
    /// Health check categories
    /// </summary>
    public enum HealthCategory
    {
        Storage,
        Configuration,
        Performance,
        Backup,
        Memory,
        DiskSpace,
        DataIntegrity,
        System
    }
    /// <summary>
    /// Health check extensions for easier usage
    /// </summary>
}