namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Supporting interfaces for audit functionality
    /// </summary>
    public interface ISyncAuditTarget
    {
        void Write(AuditLogEntry entry);
    }
}