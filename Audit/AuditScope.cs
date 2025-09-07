namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit scope for automatic operation tracking
    /// </summary>
    internal class AuditScope : IDisposable
    {
        private readonly string _operationName;
        private readonly object? _data;
        private readonly DateTime _startTime;
        private bool _disposed = false;
        private bool _success = true;
        private string? _errorMessage;

        public AuditScope(string operationName, object? data = null)
        {
            _operationName = operationName;
            _data = data;
            _startTime = DateTime.UtcNow;
        }

        public void MarkSuccess()
        {
            _success = true;
        }

        public void MarkFailure(string errorMessage)
        {
            _success = false;
            _errorMessage = errorMessage;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                var duration = DateTime.UtcNow - _startTime;

                var logger = new AuditLogger(); // This would need to be injected properly in real implementation
                Task.Run(async () =>
                {
                    if (_success)
                    {
                        await logger.LogOperationCompleteAsync(_operationName, duration, true, _data);
                    }
                    else
                    {
                        await logger.LogOperationCompleteAsync(_operationName, duration, false, new { Error = _errorMessage, OriginalData = _data });
                    }
                });
            }
        }
    }