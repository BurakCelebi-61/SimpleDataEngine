using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    /// <summary>
    /// Individual data segment
    /// </summary>
    public class Segment : IDisposable
    {
        public string Name { get; }
        public bool HasSpace => _data.Count < _maxEntries;

        private readonly string _filePath;
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;
        private readonly Dictionary<string, string> _data;
        private readonly int _maxEntries;
        private readonly object _lock = new object();
        private bool _disposed = false;
        private bool _isDirty = false;

        public Segment(string name, string filePath, HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            Name = name;
            _filePath = filePath;
            _config = config;
            _fileHandler = fileHandler;
            _data = new Dictionary<string, string>();
            _maxEntries = _config.MaxSegmentSizeMB * 1024; // Simple calculation
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            if (await _fileHandler.ExistsAsync(_filePath))
            {
                var content = await _fileHandler.ReadAllTextAsync(_filePath);
                if (!string.IsNullOrEmpty(content))
                {
                    var segmentData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                    if (segmentData != null)
                    {
                        lock (_lock)
                        {
                            _data.Clear();
                            foreach (var kvp in segmentData)
                            {
                                _data[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
        }

        public async Task StoreAsync(string id, string data)
        {
            lock (_lock)
            {
                _data[id] = data;
                _isDirty = true;
            }

            if (_config.AutoFlush)
            {
                await FlushAsync();
            }
        }

        public async Task<string?> RetrieveAsync(string id)
        {
            return await Task.FromResult(_data.TryGetValue(id, out var data) ? data : null);
        }

        public async Task FlushAsync()
        {
            if (!_isDirty) return;

            Dictionary<string, string> dataToSave;
            lock (_lock)
            {
                dataToSave = new Dictionary<string, string>(_data);
                _isDirty = false;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(dataToSave, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await _fileHandler.WriteAllTextAsync(_filePath, json);
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_isDirty)
                {
                    FlushAsync().Wait(TimeSpan.FromSeconds(10));
                }
                _disposed = true;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("SEGMENT_DISPOSE_FAILED", ex, new { SegmentName = Name });
            }
        }
    }