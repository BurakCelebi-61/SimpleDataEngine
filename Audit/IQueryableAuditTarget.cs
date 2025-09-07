namespace SimpleDataEngine.Audit
{
    public interface IQueryableAuditTarget
    {
        Task<AuditQueryResult> QueryAsync(AuditQueryOptions options);
    }
}