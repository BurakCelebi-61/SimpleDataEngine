namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Scoped audit context for tracking operations
    /// </summary>
    internal class AuditScope : IDisposable
    {
        private readonly string _eventType;
        private readonly AuditCategory _category;
        private readonly DateTime _startTime;
        private readonly Guid _scopeId;

        public AuditScope(string eventType, AuditCategory category)
        {
            _eventType = eventType;
            _category = category;
            _startTime = DateTime.Now;
            _scopeId = Guid.NewGuid();

            AuditLogger.Log($"{eventType}_STARTED", new { ScopeId = _scopeId }, category: _category);
        }

        public void Dispose()
        {
            var duration = DateTime.Now - _startTime;
            AuditLogger.LogTimed($"{_eventType}_COMPLETED", duration, new { ScopeId = _scopeId }, _category);
        }
    }
}