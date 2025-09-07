namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Interface for audit targets
    /// </summary>
    public interface IAuditTarget
    {
        /// <summary>
        /// Writes an audit log entry asynchronously
        /// </summary>
        /// <param name="entry">Audit log entry</param>
        /// <returns>Task</returns>
        Task WriteAsync(AuditLogEntry entry);
    }
}