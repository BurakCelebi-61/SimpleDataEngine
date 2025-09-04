using SimpleDataEngine.Audit;

namespace SimpleDataEngine.Notifications
{

    /// <summary>
    /// Comprehensive notification system for SimpleDataEngine
    /// </summary>
    public static class NotificationEngine
    {
        private static readonly object _lock = new object();
        private static readonly List<Notification> _inMemoryNotifications = new();
        private static readonly List<NotificationSubscription> _subscriptions = new();
        private static readonly Dictionary<NotificationChannel, INotificationHandler> _handlers = new();
        private static NotificationEngineOptions _options;
        private static System.Threading.Timer _cleanupTimer;
        private static bool _initialized = false;

        static NotificationEngine()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the notification engine
        /// </summary>
        /// <param name="options">Configuration options</param>
        public static void Initialize(NotificationEngineOptions options = null)
        {
            lock (_lock)
            {
                if (_initialized && options == null)
                    return;

                _options = options ?? new NotificationEngineOptions();
                _initialized = true;

                // Register built-in handlers
                RegisterHandler(new InMemoryNotificationHandler());
                RegisterHandler(new ConsoleNotificationHandler());
                RegisterHandler(new FileNotificationHandler());

                // Start cleanup timer if auto cleanup is enabled
                if (_options.AutoCleanupExpired)
                {
                    _cleanupTimer?.Dispose();
                    _cleanupTimer = new System.Threading.Timer(CleanupExpiredNotifications,
                        null, _options.CleanupInterval, _options.CleanupInterval);
                }

                AuditLogger.Log("NOTIFICATION_ENGINE_INITIALIZED", new { Options = _options });
            }
        }

        /// <summary>
        /// Sends a notification through the notification system
        /// </summary>
        /// <param name="notification">Notification to send</param>
        /// <returns>List of delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> SendAsync(Notification notification)
        {
            if (!_options.Enabled || notification.Severity < _options.MinimumSeverity)
            {
                return new List<NotificationDeliveryResult>();
            }

            var results = new List<NotificationDeliveryResult>();

            try
            {
                // Set expiry if not specified
                if (notification.ExpiresAt == null)
                {
                    notification.ExpiresAt = DateTime.Now.Add(_options.DefaultNotificationExpiry);
                }

                // Determine channels to send to
                var channelsToSend = notification.Channels.Any()
                    ? notification.Channels
                    : _options.DefaultChannels;

                // Get matching subscriptions
                var matchingSubscriptions = GetMatchingSubscriptions(notification);

                // Add channels from subscriptions
                foreach (var subscription in matchingSubscriptions)
                {
                    channelsToSend.AddRange(subscription.Channels);
                }

                channelsToSend = channelsToSend.Distinct().ToList();

                // Send through each channel
                foreach (var channel in channelsToSend)
                {
                    if (_handlers.ContainsKey(channel))
                    {
                        var handler = _handlers[channel];
                        if (handler.CanHandle(notification))
                        {
                            try
                            {
                                var result = await handler.SendAsync(notification);
                                results.Add(result);

                                if (!result.Success && _options.LogDeliveryFailures)
                                {
                                    AuditLogger.LogWarning("NOTIFICATION_DELIVERY_FAILED", new
                                    {
                                        NotificationId = notification.Id,
                                        Channel = channel,
                                        Error = result.ErrorMessage
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                results.Add(new NotificationDeliveryResult
                                {
                                    NotificationId = notification.Id,
                                    Channel = channel,
                                    Success = false,
                                    ErrorMessage = ex.Message
                                });

                                if (_options.LogDeliveryFailures)
                                {
                                    AuditLogger.LogError("NOTIFICATION_HANDLER_ERROR", ex, new
                                    {
                                        NotificationId = notification.Id,
                                        Channel = channel
                                    });
                                }
                            }
                        }
                    }
                }

                // Store in memory if InMemory channel is used
                if (channelsToSend.Contains(NotificationChannel.InMemory))
                {
                    StoreInMemory(notification);
                }

                AuditLogger.Log("NOTIFICATION_SENT", new
                {
                    NotificationId = notification.Id,
                    Title = notification.Title,
                    Severity = notification.Severity,
                    Category = notification.Category,
                    ChannelsCount = channelsToSend.Count,
                    SuccessfulDeliveries = results.Count(r => r.Success)
                });

                return results;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("NOTIFICATION_SEND_FAILED", ex, new { NotificationId = notification.Id });
                return results;
            }
        }

        /// <summary>
        /// Creates and sends a simple notification
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="severity">Notification severity</param>
        /// <param name="category">Notification category</param>
        /// <returns>List of delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> NotifyAsync(
            string title,
            string message,
            NotificationSeverity severity = NotificationSeverity.Info,
            NotificationCategory category = NotificationCategory.System)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity,
                Category = category,
                Source = GetCallingMethod()
            };

            return await SendAsync(notification);
        }

        /// <summary>
        /// Creates and sends a health-related notification
        /// </summary>
        /// <param name="title">Health notification title</param>
        /// <param name="message">Health notification message</param>
        /// <param name="severity">Notification severity</param>
        /// <param name="healthData">Additional health data</param>
        /// <returns>List of delivery results</returns>
        public static async Task<List<NotificationDeliveryResult>> NotifyHealthAsync(
            string title,
            string message,
            NotificationSeverity severity,
            object healthData = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity,
                Category = NotificationCategory.Health,
                Data = healthData,
                Source = "HealthChecker"
            };

            return await SendAsync(notification);
        }

        /// <summary>
        /// Gets all in-memory notifications
        /// </summary>
        /// <param name="includeRead">Whether to include read notifications</param>
        /// <param name="maxAge">Maximum age of notifications to include</param>
        /// <returns>List of notifications</returns>
        public static List<Notification> GetNotifications(bool includeRead = true, TimeSpan? maxAge = null)
        {
            lock (_lock)
            {
                var query = _inMemoryNotifications.AsQueryable();

                if (!includeRead)
                    query = query.Where(n => !n.IsRead);

                if (maxAge.HasValue)
                {
                    var cutoffDate = DateTime.Now.Subtract(maxAge.Value);
                    query = query.Where(n => n.CreatedAt >= cutoffDate);
                }

                return query.OrderByDescending(n => n.CreatedAt).ToList();
            }
        }

        /// <summary>
        /// Gets notifications by category
        /// </summary>
        /// <param name="category">Notification category</param>
        /// <param name="includeRead">Whether to include read notifications</param>
        /// <returns>List of notifications</returns>
        public static List<Notification> GetNotificationsByCategory(
            NotificationCategory category,
            bool includeRead = true)
        {
            lock (_lock)
            {
                return _inMemoryNotifications
                    .Where(n => n.Category == category && (includeRead || !n.IsRead))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets notifications by severity
        /// </summary>
        /// <param name="severity">Minimum severity level</param>
        /// <param name="includeRead">Whether to include read notifications</param>
        /// <returns>List of notifications</returns>
        public static List<Notification> GetNotificationsBySeverity(
            NotificationSeverity severity,
            bool includeRead = true)
        {
            lock (_lock)
            {
                return _inMemoryNotifications
                    .Where(n => n.Severity >= severity && (includeRead || !n.IsRead))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();
            }
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>True if notification was found and marked as read</returns>
        public static bool MarkAsRead(Guid notificationId)
        {
            lock (_lock)
            {
                var notification = _inMemoryNotifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.MarkAsRead();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Marks all notifications as read
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="maxAge">Optional maximum age filter</param>
        /// <returns>Number of notifications marked as read</returns>
        public static int MarkAllAsRead(NotificationCategory? category = null, TimeSpan? maxAge = null)
        {
            lock (_lock)
            {
                var query = _inMemoryNotifications.Where(n => !n.IsRead);

                if (category.HasValue)
                    query = query.Where(n => n.Category == category.Value);

                if (maxAge.HasValue)
                {
                    var cutoffDate = DateTime.Now.Subtract(maxAge.Value);
                    query = query.Where(n => n.CreatedAt >= cutoffDate);
                }

                var notifications = query.ToList();
                foreach (var notification in notifications)
                {
                    notification.MarkAsRead();
                }

                return notifications.Count;
            }
        }

        /// <summary>
        /// Removes a notification
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>True if notification was found and removed</returns>
        public static bool RemoveNotification(Guid notificationId)
        {
            lock (_lock)
            {
                return _inMemoryNotifications.RemoveAll(n => n.Id == notificationId) > 0;
            }
        }

        /// <summary>
        /// Clears all notifications
        /// </summary>
        /// <param name="olderThan">Optional date filter - only remove notifications older than this</param>
        /// <returns>Number of notifications removed</returns>
        public static int ClearNotifications(DateTime? olderThan = null)
        {
            lock (_lock)
            {
                if (olderThan == null)
                {
                    var count = _inMemoryNotifications.Count;
                    _inMemoryNotifications.Clear();
                    return count;
                }
                else
                {
                    return _inMemoryNotifications.RemoveAll(n => n.CreatedAt < olderThan.Value);
                }
            }
        }

        /// <summary>
        /// Gets notification statistics
        /// </summary>
        /// <returns>Notification statistics</returns>
        public static NotificationStatistics GetStatistics()
        {
            lock (_lock)
            {
                var stats = new NotificationStatistics();

                if (!_inMemoryNotifications.Any())
                    return stats;

                stats.TotalNotifications = _inMemoryNotifications.Count;
                stats.UnreadNotifications = _inMemoryNotifications.Count(n => !n.IsRead);
                stats.ExpiredNotifications = _inMemoryNotifications.Count(n => n.IsExpired);
                stats.OldestNotification = _inMemoryNotifications.Min(n => n.CreatedAt);
                stats.NewestNotification = _inMemoryNotifications.Max(n => n.CreatedAt);

                var timeSpan = stats.NewestNotification - stats.OldestNotification;
                stats.AverageNotificationsPerDay = timeSpan.TotalDays > 0
                    ? stats.TotalNotifications / timeSpan.TotalDays
                    : 0;

                stats.CountsBySeverity = _inMemoryNotifications
                    .GroupBy(n => n.Severity)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.CountsByCategory = _inMemoryNotifications
                    .GroupBy(n => n.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.CountsByChannel = _inMemoryNotifications
                    .SelectMany(n => n.Channels)
                    .GroupBy(c => c)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.ActiveSubscriptions = _subscriptions.Count(s => s.Enabled);

                return stats;
            }
        }

        /// <summary>
        /// Registers a notification handler
        /// </summary>
        /// <param name="handler">Notification handler</param>
        public static void RegisterHandler(INotificationHandler handler)
        {
            lock (_lock)
            {
                _handlers[handler.Channel] = handler;
                AuditLogger.Log("NOTIFICATION_HANDLER_REGISTERED", new
                {
                    Channel = handler.Channel,
                    Name = handler.Name
                });
            }
        }

        /// <summary>
        /// Subscribes to notifications with specified criteria
        /// </summary>
        /// <param name="subscription">Notification subscription</param>
        /// <returns>Subscription ID</returns>
        public static string Subscribe(NotificationSubscription subscription)
        {
            lock (_lock)
            {
                _subscriptions.Add(subscription);
                AuditLogger.Log("NOTIFICATION_SUBSCRIPTION_CREATED", new
                {
                    SubscriptionId = subscription.Id,
                    Name = subscription.Name,
                    Categories = subscription.Categories,
                    Severities = subscription.Severities,
                    Channels = subscription.Channels
                });
                return subscription.Id;
            }
        }

        /// <summary>
        /// Unsubscribes from notifications
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <returns>True if subscription was found and removed</returns>
        public static bool Unsubscribe(string subscriptionId)
        {
            lock (_lock)
            {
                var removed = _subscriptions.RemoveAll(s => s.Id == subscriptionId) > 0;
                if (removed)
                {
                    AuditLogger.Log("NOTIFICATION_SUBSCRIPTION_REMOVED", new { SubscriptionId = subscriptionId });
                }
                return removed;
            }
        }

        /// <summary>
        /// Gets all active subscriptions
        /// </summary>
        /// <returns>List of subscriptions</returns>
        public static List<NotificationSubscription> GetSubscriptions()
        {
            lock (_lock)
            {
                return _subscriptions.ToList();
            }
        }

        #region Private Methods

        private static void StoreInMemory(Notification notification)
        {
            lock (_lock)
            {
                _inMemoryNotifications.Add(notification);

                // Enforce maximum in-memory notifications limit
                if (_inMemoryNotifications.Count > _options.MaxInMemoryNotifications)
                {
                    var toRemove = _inMemoryNotifications.Count - _options.MaxInMemoryNotifications;
                    var oldestNotifications = _inMemoryNotifications
                        .OrderBy(n => n.CreatedAt)
                        .Take(toRemove)
                        .ToList();

                    foreach (var old in oldestNotifications)
                    {
                        _inMemoryNotifications.Remove(old);
                    }
                }
            }
        }

        private static List<NotificationSubscription> GetMatchingSubscriptions(Notification notification)
        {
            lock (_lock)
            {
                return _subscriptions
                    .Where(s => s.ShouldReceive(notification))
                    .ToList();
            }
        }

        private static void CleanupExpiredNotifications(object state)
        {
            try
            {
                lock (_lock)
                {
                    var expiredCount = _inMemoryNotifications.RemoveAll(n => n.IsExpired);

                    if (expiredCount > 0)
                    {
                        AuditLogger.Log("NOTIFICATIONS_CLEANUP", new { ExpiredCount = expiredCount });
                    }
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("NOTIFICATION_CLEANUP_FAILED", ex);
            }
        }

        private static string GetCallingMethod()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                var frame = stackTrace.GetFrame(2);
                return $"{frame.GetMethod().DeclaringType?.Name}.{frame.GetMethod().Name}";
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion

        /// <summary>
        /// Disposes resources used by the notification engine
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                _cleanupTimer?.Dispose();
                AuditLogger.Log("NOTIFICATION_ENGINE_DISPOSED");
            }
        }
    }

    #region Built-in Notification Handlers

    /// <summary>
    /// In-memory notification handler (default)
    /// </summary>
        #endregion

    /// <summary>
    /// Extension methods for easier notification usage
    /// </summary>
}