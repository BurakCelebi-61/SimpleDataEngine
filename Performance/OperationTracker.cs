namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Operation tracker for measuring performance
    /// </summary>
    public class OperationTracker : IDisposable
    {
        private readonly string _operationName;
        private readonly string _category;
        private readonly DateTime _startTime;
        private bool _success = false;
        private bool _disposed = false;

        public OperationTracker(string operationName, string category)
        {
            _operationName = operationName;
            _category = category;
            _startTime = DateTime.UtcNow;
        }

        public void MarkSuccess()
        {
            _success = true;
        }

        public void MarkFailure()
        {
            _success = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                PerformanceReportGenerator.RecordMetric(_operationName, duration, _category, _success);
                _disposed = true;
            }
        }
    }
}