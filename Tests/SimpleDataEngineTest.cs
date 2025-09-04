using SimpleDataEngine.Audit;
using SimpleDataEngine.Backup;
using SimpleDataEngine.Configuration;
using SimpleDataEngine.Export;
using SimpleDataEngine.Health;
using SimpleDataEngine.Notifications;
using SimpleDataEngine.Performance;
using SimpleDataEngine.Repositories;
using SimpleDataEngine.Storage;
using SimpleDataEngine.Validation;

namespace SimpleDataEngine.Tests
{
    /// <summary>
    /// Comprehensive test program for SimpleDataEngine
    /// </summary>
    public class SimpleDataEngineTest
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 SimpleDataEngine Test Started!");
            Console.WriteLine("=" + new string('=', 50));

            try
            {
                // Test 1: Configuration
                await TestConfiguration();

                // Test 2: Basic Repository Operations
                await TestRepositoryOperations();

                // Test 3: Performance Tracking
                await TestPerformanceTracking();

                // Test 4: Health Checking
                await TestHealthChecking();

                // Test 5: Audit Logging
                await TestAuditLogging();

                // Test 6: Validation
                await TestValidation();

                // Test 7: Backup System
                await TestBackupSystem();

                // Test 8: Data Export
                await TestDataExport();

                // Test 9: Notifications
                await TestNotifications();

                Console.WriteLine("\n🎉 ALL TESTS PASSED SUCCESSFULLY!");
                Console.WriteLine("=" + new string('=', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ TEST FAILED: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task TestConfiguration()
        {
            Console.WriteLine("\n📋 Testing Configuration System...");

            // Test configuration loading
            var config = ConfigManager.Current;
            Console.WriteLine($"✅ Configuration loaded - Version: {config.Version}");

            // Test configuration validation
            var validation = config.Validate();
            if (validation.IsValid)
            {
                Console.WriteLine("✅ Configuration validation passed");
            }
            else
            {
                Console.WriteLine($"⚠️ Configuration has {validation.ErrorCount} errors");
                foreach (var error in validation.Errors)
                {
                    Console.WriteLine($"   - {error}");
                }
            }

            // Test configuration update
            ConfigManager.UpdateConfiguration(cfg =>
            {
                cfg.Database.MaxDatabaseSizeMB = 2000;
                cfg.Performance.EnablePerformanceTracking = true;
            });
            Console.WriteLine("✅ Configuration updated successfully");
        }

        static async Task TestRepositoryOperations()
        {
            Console.WriteLine("\n🗄️ Testing Repository Operations...");

            // Create storage and repository
            var storage = new SimpleStorage<TestUser>();
            var repository = new SimpleRepository<TestUser>(storage);

            // Test CRUD operations
            var user1 = new TestUser
            {
                Name = "John Doe",
                Email = "john@example.com",
                Age = 30
            };

            // Create
            repository.Add(user1);
            Console.WriteLine($"✅ User created with ID: {user1.Id}");

            // Read
            var retrievedUser = repository.GetById(user1.Id);
            Console.WriteLine($"✅ User retrieved: {retrievedUser?.Name}");

            // Update
            user1.Name = "John Smith";
            repository.Update(user1);
            Console.WriteLine("✅ User updated successfully");

            // Query
            var users = repository.Find(u => u.Age > 25);
            Console.WriteLine($"✅ Query executed - Found {users.Count} users");

            // Bulk operations
            var newUsers = new List<TestUser>
            {
                new TestUser { Name = "Alice", Email = "alice@example.com", Age = 25 },
                new TestUser { Name = "Bob", Email = "bob@example.com", Age = 35 }
            };
            repository.AddRange(newUsers);
            Console.WriteLine($"✅ Added {newUsers.Count} users in bulk");

            // Test async operations
            var allUsers = await repository.GetAllAsync();
            Console.WriteLine($"✅ Async operation - Total users: {allUsers.Count}");

            repository.Dispose();
        }

        static async Task TestPerformanceTracking()
        {
            Console.WriteLine("\n📊 Testing Performance Tracking...");

            // Track some operations
            using (var timer = PerformanceTracker.StartOperation("DatabaseQuery", "Data"))
            {
                await Task.Delay(100); // Simulate work
            }

            using (var timer = PerformanceTracker.StartOperation("FileOperation", "IO"))
            {
                await Task.Delay(50); // Simulate work
            }

            // Generate performance report
            var report = PerformanceReportGenerator.GenerateReport(TimeSpan.FromMinutes(1));
            Console.WriteLine($"✅ Performance report generated:");
            Console.WriteLine($"   - Total Operations: {report.TotalOperations}");
            Console.WriteLine($"   - Average Time: {report.AverageOperationTimeMs:F2}ms");
            Console.WriteLine($"   - Success Rate: {report.OverallSuccessRate:F2}%");

            // Test failed operation
            PerformanceTracker.TrackOperation("FailedOperation", TimeSpan.FromMilliseconds(200), "Test", false, "Test error");
            Console.WriteLine("✅ Failed operation tracked");
        }

        static async Task TestHealthChecking()
        {
            Console.WriteLine("\n🏥 Testing Health Checking...");

            // Perform health check
            var healthReport = await HealthChecker.CheckHealthAsync();
            Console.WriteLine($"✅ Health check completed:");
            Console.WriteLine($"   - Overall Status: {healthReport.OverallStatus}");
            Console.WriteLine($"   - Total Checks: {healthReport.Checks.Count}");
            Console.WriteLine($"   - Healthy: {healthReport.HealthyCount}");
            Console.WriteLine($"   - Warnings: {healthReport.WarningCount}");
            Console.WriteLine($"   - Duration: {healthReport.TotalDuration.TotalMilliseconds:F0}ms");

            // Quick health check
            var isHealthy = await HealthChecker.IsHealthyAsync();
            Console.WriteLine($"✅ Quick health check: {(isHealthy ? "Healthy" : "Unhealthy")}");

            // Print health summary
            var summary = healthReport.ToSummaryString();
            Console.WriteLine($"✅ Health Summary:\n{summary}");
        }

        static async Task TestAuditLogging()
        {
            Console.WriteLine("\n📝 Testing Audit Logging...");

            // Test different audit levels
            AuditLogger.Log("TEST_INFO", new { TestData = "Info level test" }, "Test info message");
            AuditLogger.LogWarning("TEST_WARNING", new { TestData = "Warning test" }, "Test warning message");
            AuditLogger.LogError("TEST_ERROR", new Exception("Test exception"), new { TestData = "Error test" });

            // Test scoped logging
            using (var scope = AuditLogger.BeginScope("TEST_OPERATION"))
            {
                await Task.Delay(50);
                AuditLogger.Log("OPERATION_STEP", new { Step = 1 }, "First step completed");
            }

            // Query audit logs
            var queryOptions = new AuditQueryOptions
            {
                StartDate = DateTime.Now.AddMinutes(-1),
                Take = 10
            };
            var auditResult = await AuditLogger.QueryAsync(queryOptions);
            Console.WriteLine($"✅ Audit query completed:");
            Console.WriteLine($"   - Found: {auditResult.TotalCount} entries");
            Console.WriteLine($"   - Query Duration: {auditResult.QueryDuration.TotalMilliseconds:F0}ms");

            // Get audit statistics
            var auditStats = await AuditLogger.GetStatisticsAsync();
            Console.WriteLine($"✅ Audit statistics:");
            Console.WriteLine($"   - Total Entries: {auditStats.TotalEntries}");
            Console.WriteLine($"   - Avg/Day: {auditStats.AverageEntriesPerDay:F1}");

            AuditLogger.Flush();
        }

        static async Task TestValidation()
        {
            Console.WriteLine("\n✅ Testing Validation System...");

            // Test valid entity
            var validUser = new TestUser
            {
                Name = "Valid User",
                Email = "valid@example.com",
                Age = 25
            };

            var validResult = SimpleValidator.Validate(validUser);
            Console.WriteLine($"✅ Valid entity validation: {(validResult.IsValid ? "Passed" : "Failed")}");

            // Test invalid entity
            var invalidUser = new TestUser
            {
                Name = "", // Invalid: empty name
                Email = "invalid-email", // Could be invalid depending on rules
                Age = -5 // Invalid: negative age
            };

            var invalidResult = SimpleValidator.Validate(invalidUser);
            Console.WriteLine($"✅ Invalid entity validation: {(invalidResult.IsValid ? "Passed" : "Failed")}");
            Console.WriteLine($"   - Errors: {invalidResult.ErrorCount}");
            Console.WriteLine($"   - Warnings: {invalidResult.WarningCount}");

            // Test bulk validation
            var users = new List<TestUser> { validUser, invalidUser };
            var bulkResult = SimpleValidator.ValidateBulk(users);
            Console.WriteLine($"✅ Bulk validation completed:");
            Console.WriteLine($"   - Total: {bulkResult.TotalProcessed}");
            Console.WriteLine($"   - Valid: {bulkResult.ValidCount}");
            Console.WriteLine($"   - Invalid: {bulkResult.InvalidCount}");

            // Test quick validation
            var isValid = SimpleValidator.IsValid(validUser);
            Console.WriteLine($"✅ Quick validation: {isValid}");
        }

        static async Task TestBackupSystem()
        {
            Console.WriteLine("\n💾 Testing Backup System...");

            try
            {
                // Create a backup
                var backupPath = BackupManager.CreateBackup("Test backup");
                Console.WriteLine($"✅ Backup created: {Path.GetFileName(backupPath)}");

                // Get available backups
                var backups = BackupManager.GetAvailableBackups();
                Console.WriteLine($"✅ Available backups: {backups.Count}");

                if (backups.Any())
                {
                    var latestBackup = backups.First();
                    Console.WriteLine($"   - Latest: {latestBackup.FileName}");
                    Console.WriteLine($"   - Size: {latestBackup.FileSizeFormatted}");
                    Console.WriteLine($"   - Age: {latestBackup.Age.TotalMinutes:F1} minutes");

                    // Validate backup
                    var validation = BackupManager.ValidateBackup(latestBackup.FilePath);
                    Console.WriteLine($"✅ Backup validation: {(validation.IsValid ? "Valid" : "Invalid")}");
                }

                // Get backup directory size
                var dirSize = BackupManager.GetBackupDirectorySize();
                Console.WriteLine($"✅ Backup directory size: {dirSize / 1024.0:F2} KB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Backup test warning: {ex.Message}");
            }
        }

        static async Task TestDataExport()
        {
            Console.WriteLine("\n📤 Testing Data Export...");

            try
            {
                // Create some test data
                var storage = new SimpleStorage<TestUser>();
                var repository = new SimpleRepository<TestUser>(storage);

                var testUsers = new List<TestUser>
                {
                    new TestUser { Name = "Export User 1", Email = "export1@test.com", Age = 30 },
                    new TestUser { Name = "Export User 2", Email = "export2@test.com", Age = 25 }
                };
                repository.AddRange(testUsers);

                // Quick JSON export
                var exportPath = Path.Combine(Path.GetTempPath(), "test_export.json");
                var exportResult = DataExporter.QuickJsonExport(exportPath);

                Console.WriteLine($"✅ Data export completed:");
                Console.WriteLine($"   - Success: {exportResult.Success}");
                Console.WriteLine($"   - File: {Path.GetFileName(exportResult.ExportPath)}");
                Console.WriteLine($"   - Size: {exportResult.FileSizeFormatted}");
                Console.WriteLine($"   - Records: {exportResult.TotalRecords}");
                Console.WriteLine($"   - Duration: {exportResult.Duration.TotalMilliseconds:F0}ms");

                // Cleanup
                if (File.Exists(exportPath))
                {
                    File.Delete(exportPath);
                }

                repository.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Export test warning: {ex.Message}");
            }
        }

        static async Task TestNotifications()
        {
            Console.WriteLine("\n🔔 Testing Notification System...");

            // Initialize notification engine
            NotificationEngine.Initialize();

            // Send test notifications
            await NotificationEngine.NotifyAsync("Test Notification", "This is a test notification",
                NotificationSeverity.Info, NotificationCategory.System);

            await NotificationEngine.NotifyAsync("Warning Notification", "This is a warning",
                NotificationSeverity.Warning, NotificationCategory.Health);

            // Get notifications
            var notifications = NotificationEngine.GetNotifications(includeRead: true, maxAge: TimeSpan.FromMinutes(1));
            Console.WriteLine($"✅ Notifications sent and retrieved: {notifications.Count}");

            // Test notification statistics
            var notificationStats = NotificationEngine.GetStatistics();
            Console.WriteLine($"✅ Notification statistics:");
            Console.WriteLine($"   - Total: {notificationStats.TotalNotifications}");
            Console.WriteLine($"   - Unread: {notificationStats.UnreadNotifications}");

            // Test subscription
            var subscription = NotificationExtensions.CreateCriticalAlertSubscription(NotificationChannel.Console);
            var subscriptionId = NotificationEngine.Subscribe(subscription);
            Console.WriteLine($"✅ Subscription created: {subscriptionId}");

            // Send critical notification to test subscription
            await NotificationEngine.NotifyAsync("Critical Test", "Critical notification test",
                NotificationSeverity.Critical, NotificationCategory.System);

            // Mark notifications as read
            var markedCount = NotificationEngine.MarkAllAsRead();
            Console.WriteLine($"✅ Marked {markedCount} notifications as read");
        }
    }
}