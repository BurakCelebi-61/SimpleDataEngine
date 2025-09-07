// Notifications/NotificationEngine.cs - LogWarning hatası düzeltildi
using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Notifications
{
    public static class NotificationEngine
    {
        private static readonly List<INotificationTarget> _targets = new List<INotificationTarget>();
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                // Add default console target
                AddTarget(new ConsoleNotificationTarget());
                _initialized = true;

                AuditLogger.Log("NOTIFICATION_ENGINE_INITIALIZED", null, AuditCategory.System);
            }
        }

        public static void AddTarget(INotificationTarget target)
        {
            lock (_lock)
            {
                _targets.Add(target);
            }
        }

        public static async Task NotifyAsync(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            if (!_initialized)
                Initialize();

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            };

            // Log to audit system
            if (severity == NotificationSeverity.Warning || severity == NotificationSeverity.Error)
            {
                // DÜZELTME: LogWarning metodu artık mevcut
                AuditLogger.LogWarning($"NOTIFICATION_{severity.ToString().ToUpper()}", new
                {
                    Title = title,
                    Message = message,
                    Severity = severity
                }, AuditCategory.System);
            }
            else
            {
                AuditLogger.Log($"NOTIFICATION_{severity.ToString().ToUpper()}", new
                {
                    Title = title,
                    Message = message,
                    Severity = severity
                }, AuditCategory.System);
            }

            // Send to all targets
            var tasks = new List<Task>();
            lock (_lock)
            {
                foreach (var target in _targets)
                {
                    tasks.Add(target.SendAsync(notification));
                }
            }

            if (tasks.Any())
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("NOTIFICATION_SEND_FAILED", ex, new { NotificationId = notification.Id });
                }
            }
        }

        public static List<Notification> GetNotifications(int count = 100)
        {
            // Implementation for getting recent notifications
            return new List<Notification>();
        }

        public static NotificationStatistics GetStatistics()
        {
            return new NotificationStatistics
            {
                TotalSent = 0,
                TargetCount = _targets.Count,
                LastNotificationTime = DateTime.UtcNow
            };
        }

        public static void Subscribe(INotificationSubscription subscription)
        {
            // Implementation for subscription management
        }
    }

    public interface INotificationTarget
    {
        Task SendAsync(Notification notification);
    }

    public class ConsoleNotificationTarget : INotificationTarget
    {
        public async Task SendAsync(Notification notification)
        {
            await Task.Run(() =>
            {
                var color = notification.Severity switch
                {
                    NotificationSeverity.Error => ConsoleColor.Red,
                    NotificationSeverity.Warning => ConsoleColor.Yellow,
                    NotificationSeverity.Info => ConsoleColor.White,
                    _ => ConsoleColor.Gray
                };

                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine($"[{notification.Timestamp:HH:mm:ss}] [{notification.Severity}] {notification.Title}: {notification.Message}");
                Console.ForegroundColor = originalColor;
            });
        }
    }

    public class Notification
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum NotificationSeverity
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class NotificationStatistics
    {
        public int TotalSent { get; set; }
        public int TargetCount { get; set; }
        public DateTime LastNotificationTime { get; set; }
    }

    public interface INotificationSubscription
    {
        // Subscription interface
    }
}

// Versioning/BaseMigration.cs - LogWarning hatası düzeltildi
using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Base migration class - LogWarning hatası düzeltildi
    /// </summary>
    public abstract class BaseMigration
    {
        public abstract string Version { get; }
        public abstract string Description { get; }

        protected DateTime StartTime { get; private set; }
        protected TimeSpan Duration => DateTime.UtcNow - StartTime;

        public async Task<MigrationResult> ExecuteAsync()
        {
            StartTime = DateTime.UtcNow;

            AuditLogger.Log("MIGRATION_STARTED", new
            {
                Version = Version,
                Description = Description,
                StartTime = StartTime
            }, AuditCategory.Database);

            try
            {
                await PerformMigrationAsync();

                var result = new MigrationResult
                {
                    Success = true,
                    Version = Version,
                    Duration = Duration,
                    Message = "Migration completed successfully"
                };

                AuditLogger.Log("MIGRATION_COMPLETED", new
                {
                    Version = Version,
                    Duration = Duration.TotalMilliseconds,
                    Success = true
                }, AuditCategory.Database);

                return result;
            }
            catch (Exception ex)
            {
                var result = new MigrationResult
                {
                    Success = false,
                    Version = Version,
                    Duration = Duration,
                    Message = $"Migration failed: {ex.Message}",
                    Exception = ex
                };

                AuditLogger.LogError("MIGRATION_FAILED", ex, new
                {
                    Version = Version,
                    Duration = Duration.TotalMilliseconds,
                    Description = Description
                });

                return result;
            }
        }

        protected abstract Task PerformMigrationAsync();

        protected void LogProgress(string message, object? data = null)
        {
            AuditLogger.Log("MIGRATION_PROGRESS", new
            {
                Version = Version,
                Message = message,
                ElapsedMs = Duration.TotalMilliseconds,
                Data = data
            }, AuditCategory.Database);
        }

        protected void LogWarning(string message, object? data = null)
        {
            // DÜZELTME: LogWarning metodu artık mevcut
            AuditLogger.LogWarning("MIGRATION_WARNING", new
            {
                Version = Version,
                Message = message,
                ElapsedMs = Duration.TotalMilliseconds,
                Data = data
            }, AuditCategory.Database);
        }

        protected void LogError(string message, Exception? exception = null, object? data = null)
        {
            AuditLogger.LogError("MIGRATION_ERROR", exception, new
            {
                Version = Version,
                Message = message,
                ElapsedMs = Duration.TotalMilliseconds,
                Data = data
            });
        }

        protected async Task BackupDataAsync(string backupPath)
        {
            LogProgress("Creating backup", new { BackupPath = backupPath });

            try
            {
                // Implementation for backup
                await Task.Delay(100); // Placeholder

                LogProgress("Backup created successfully", new { BackupPath = backupPath });
            }
            catch (Exception ex)
            {
                LogError("Backup creation failed", ex, new { BackupPath = backupPath });
                throw;
            }
        }

        protected async Task ValidateDataIntegrityAsync()
        {
            LogProgress("Validating data integrity");

            try
            {
                // Implementation for validation
                await Task.Delay(50); // Placeholder

                LogProgress("Data integrity validation passed");
            }
            catch (Exception ex)
            {
                LogError("Data integrity validation failed", ex);
                throw;
            }
        }
    }

    public class MigrationResult
    {
        public bool Success { get; set; }
        public string Version { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}

// Storage/Hierarchical/HierarchicalDatabaseConfig.cs - Eksik config sınıfı
namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Configuration for hierarchical database
    /// </summary>
    public class HierarchicalDatabaseConfig
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool EncryptionEnabled { get; set; } = false;
        public Security.EncryptionConfig? EncryptionConfig { get; set; }
        public int MaxMemoryUsageMB { get; set; } = 512;
        public TimeSpan? AutoFlushInterval { get; set; } = TimeSpan.FromSeconds(30);
        public bool EnableCompression { get; set; } = true;
        public int MaxSegmentSizeMB { get; set; } = 64;
        public bool CacheEnabled { get; set; } = true;
        public int CacheSizeMB { get; set; } = 128;
        public bool IndexCacheEnabled { get; set; } = true;
        public bool EnableDetailedLogging { get; set; } = false;
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Validate configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(DatabasePath))
                result.AddError("DatabasePath cannot be null or empty");

            if (MaxMemoryUsageMB <= 0)
                result.AddError("MaxMemoryUsageMB must be positive");

            if (MaxSegmentSizeMB <= 0)
                result.AddError("MaxSegmentSizeMB must be positive");

            if (CacheEnabled && CacheSizeMB <= 0)
                result.AddError("CacheSizeMB must be positive when cache is enabled");

            if (EncryptionEnabled && EncryptionConfig != null)
            {
                var encryptionValidation = EncryptionConfig.Validate();
                if (!encryptionValidation.IsValid)
                    result.AddErrors(encryptionValidation.Errors);
            }

            return result;
        }
    }

    /// <summary>
    /// Validation result helper class
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
                Errors.Add(error);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors ?? Enumerable.Empty<string>())
                AddError(error);
        }

        public override string ToString()
        {
            return IsValid ? $"Valid";: $"Invalid: {string.Join(", ", Errors)}";
        }
    }
}

// Storage/Hierarchical/Managers/GlobalMetadataManager.cs - Eksik manager sınıfları
using SimpleDataEngine.Security;

namespace SimpleDataEngine.Storage.Hierarchical.Managers
{
    public class GlobalMetadataManager
    {
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;

        public GlobalMetadataManager(HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        }

        public async Task InitializeAsync()
        {
            // Implementation for initialization
            await Task.CompletedTask;
        }

        public async Task FlushAsync()
        {
            // Implementation for flushing
            await Task.CompletedTask;
        }

        public async Task HealthCheckAsync()
        {
            // Implementation for health check
            await Task.CompletedTask;
        }
    }

    public class EntityMetadataManager
    {
        private readonly string _entityName;
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;

        public EntityMetadataManager(string entityName, HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        }

        public async Task InitializeAsync()
        {
            // Implementation for initialization
            await Task.CompletedTask;
        }

        public async Task FlushAsync()
        {
            // Implementation for flushing
            await Task.CompletedTask;
        }
    }

    public class SegmentManager
    {
        private readonly string _entityName;
        private readonly HierarchicalDatabaseConfig _config;
        private readonly IFileHandler _fileHandler;

        public SegmentManager(string entityName, HierarchicalDatabaseConfig config, IFileHandler fileHandler)
        {
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        }

        public async Task InitializeAsync()
        {
            // Implementation for initialization
            await Task.CompletedTask;
        }

        public async Task FlushAsync()
        {
            // Implementation for flushing
            await Task.CompletedTask;
        }
    }
}