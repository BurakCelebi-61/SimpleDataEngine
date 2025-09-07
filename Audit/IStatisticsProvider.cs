namespace SimpleDataEngine.Audit
{
    public interface IStatisticsProvider
    {
        Task<AuditStatistics> GetStatisticsAsync();
    }
}