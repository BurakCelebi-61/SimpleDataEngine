// PerformanceTracker.cs içinde zaten var:

using SimpleDataEngine.Performance;

/// <summary>
/// Static operation timer for tracking operation duration
/// </summary>
internal class StaticOperationTimer : IDisposable
{
    private readonly string _operation;
    private readonly string _category;
    private readonly DateTime _startTime;
    private bool _disposed;

    public StaticOperationTimer(string operation, string category)
    {
        _operation = operation;
        _category = category;
        _startTime = DateTime.Now;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            var duration = DateTime.Now - _startTime;
            PerformanceTracker.TrackOperation(_operation, duration, _category);
            _disposed = true;
        }
    }
}
