namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Scoped audit context for tracking operations - LogTimed metodu düzeltildi
    /// </summary>
    internal class AuditScope : IDisposable
    {
        private readonly string _eventType;
        private readonly AuditCategory _category;
        private readonly DateTime _startTime;
        private readonly Guid _scopeId;
        private bool _disposed = false;

        public AuditScope(string eventType, AuditCategory category = AuditCategory.General)
        {
            _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            _category = category;
            _startTime = DateTime.UtcNow;
            _scopeId = Guid.NewGuid();

            // Log scope start
            AuditLogger.Log($"{eventType}_STARTED", new { ScopeId = _scopeId, StartTime = _startTime }, _category);
        }

        /// <summary>
        /// Log an event within this scope
        /// </summary>
        public void LogEvent(string message, object data = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AuditScope));

            AuditLogger.Log($"{_eventType}_{message}", new { ScopeId = _scopeId, Data = data }, _category);
        }

        /// <summary>
        /// Log a warning within this scope
        /// </summary>
        public void LogWarning(string message, object data = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AuditScope));

            AuditLogger.LogWarning($"{_eventType}_{message}", new { ScopeId = _scopeId, Data = data }, _category);
        }

        /// <summary>
        /// Log an error within this scope
        /// </summary>
        public void LogError(string message, Exception exception = null, object data = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AuditScope));

            AuditLogger.LogError($"{_eventType}_{message}", exception, new { ScopeId = _scopeId, Data = data }, _category);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    var duration = DateTime.UtcNow - _startTime;

                    // LogTimed metodu artık mevcut
                    AuditLogger.LogTimed($"{_eventType}_COMPLETED", duration, new { ScopeId = _scopeId }, _category);
                }
                catch (Exception ex)
                {
                    // Fallback logging if LogTimed fails
                    Console.WriteLine($"[AUDIT SCOPE ERROR] Failed to log scope completion: {ex.Message}");
                    try
                    {
                        var duration = DateTime.UtcNow - _startTime;
                        AuditLogger.Log($"{_eventType}_COMPLETED", new
                        {
                            ScopeId = _scopeId,
                            DurationMs = duration.TotalMilliseconds,
                            Duration = duration.ToString(),
                            Error = "LogTimed method failed"
                        }, _category);
                    }
                    catch
                    {
                        // Final fallback
                        Console.WriteLine($"[AUDIT SCOPE FALLBACK] Scope {_eventType} completed in {(DateTime.UtcNow - _startTime).TotalMilliseconds:F2}ms");
                    }
                }

                _disposed = true;
            }
        }
    }
}