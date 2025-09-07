using SimpleDataEngine.Audit;
using SimpleDataEngine.Performance;

namespace SimpleDataEngine.Tests
{
    /// <summary>
    /// Test class for SimpleDataEngine - Fixed method calls
    /// </summary>
    public static class SimpleDataEngineTest
    {
        /// <summary>
        /// Run comprehensive tests
        /// </summary>
        public static async Task RunAllTestsAsync()
        {
            Console.WriteLine("Starting SimpleDataEngine Tests...");

            try
            {
                await TestAuditLogging();
                await TestPerformanceTracking();
                await TestDataOperations();

                Console.WriteLine("All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                AuditLogger.LogError("TEST_FAILED", ex);
            }
        }

        /// <summary>
        /// Test audit logging functionality
        /// </summary>
        private static async Task TestAuditLogging()
        {
            Console.WriteLine("Testing Audit Logging...");

            // Test basic logging
            AuditLogger.Log("TEST_START", new { TestName = "AuditLogging" }, AuditCategory.General);

            // Test warning logging - FIXED: Now using correct method
            AuditLogger.LogWarning("TEST_WARNING", new { Message = "This is a test warning" }, AuditCategory.General);

            // Test scoped logging - FIXED: Now using correct method
            using (var scope = AuditLogger.BeginScope("TestScope", new { ScopeData = "TestData" }))
            {
                AuditLogger.Log("INSIDE_SCOPE", null, AuditCategory.General);
            }

            // Test query functionality - FIXED: Now using correct method
            var queryOptions = new AuditQueryOptions
            {
                StartDate = DateTime.UtcNow.AddMinutes(-5),
                Take = 10
            };

            var queryResult = await AuditLogger.QueryAsync(queryOptions);
            Console.WriteLine($"Query returned {queryResult.Entries.Count} entries");

            Console.WriteLine("Audit logging tests completed.");
        }

        /// <summary>
        /// Test performance tracking functionality
        /// </summary>
        private static async Task TestPerformanceTracking()
        {
            Console.WriteLine("Testing Performance Tracking...");

            // Record some test metrics
            PerformanceReportGenerator.RecordMetric("TestOperation1", TimeSpan.FromMilliseconds(100), "Testing", true);
            PerformanceReportGenerator.RecordMetric("TestOperation2", TimeSpan.FromMilliseconds(200), "Testing", true);
            PerformanceReportGenerator.RecordMetric("TestOperation3", TimeSpan.FromMilliseconds(300), "Testing", false);

            // Test operation tracking
            using (var tracker = PerformanceTracker.StartOperation("TestTrackedOperation", "Testing"))
            {
                await Task.Delay(50);
                tracker.MarkSuccess();
            }

            // Generate report - FIXED: Now using correct method
            var report = PerformanceReportGenerator.GenerateReport(TimeSpan.FromMinutes(5));

            Console.WriteLine($"Performance Report:");
            Console.WriteLine($"  Total Operations: {report.TotalOperations}");
            Console.WriteLine($"  Success Rate: {report.OverallSuccessRate:F1}%");
            Console.WriteLine($"  Average Time: {report.AverageOperationTimeMs:F2}ms");

            Console.WriteLine("Performance tracking tests completed.");
        }

        /// <summary>
        /// Test basic data operations
        /// </summary>
        private static async Task TestDataOperations()
        {
            Console.WriteLine("Testing Data Operations...");

            // Test data serialization and deserialization
            var testData = new TestEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test Entity",
                Value = 42,
                CreatedAt = DateTime.UtcNow
            };

            var json = System.Text.Json.JsonSerializer.Serialize(testData);
            var deserializedData = System.Text.Json.JsonSerializer.Deserialize<TestEntity>(json);

            if (deserializedData != null && deserializedData.Name == testData.Name)
            {
                Console.WriteLine("Data serialization test passed.");
            }
            else
            {
                throw new Exception("Data serialization test failed.");
            }

            // Test async operations
            await Task.Delay(10);

            Console.WriteLine("Data operations tests completed.");
        }

        /// <summary>
        /// Test flush functionality - FIXED: Now using correct method
        /// </summary>
        private static void TestFlushFunctionality()
        {
            Console.WriteLine("Testing Flush Functionality...");

            // Test audit flush
            AuditLogger.Flush();

            Console.WriteLine("Flush functionality tests completed.");
        }

        /// <summary>
        /// Run performance benchmark
        /// </summary>
        public static async Task RunPerformanceBenchmarkAsync()
        {
            Console.WriteLine("Running Performance Benchmark...");

            const int operationCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < operationCount; i++)
            {
                using (var tracker = PerformanceTracker.StartOperation($"BenchmarkOp_{i}", "Benchmark"))
                {
                    // Simulate some work
                    await Task.Delay(1);
                    tracker.MarkSuccess();
                }

                if (i % 100 == 0)
                {
                    Console.WriteLine($"Completed {i}/{operationCount} operations");
                }
            }

            stopwatch.Stop();

            var report = PerformanceReportGenerator.GenerateReport(TimeSpan.FromMinutes(5));

            Console.WriteLine($"Benchmark Results:");
            Console.WriteLine($"  Total Operations: {report.TotalOperations}");
            Console.WriteLine($"  Total Time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Average Time per Operation: {report.AverageOperationTimeMs:F2}ms");
            Console.WriteLine($"  Operations per Second: {report.OperationsPerSecond:F0}");

            Console.WriteLine("Performance benchmark completed.");
        }
    }

    /// <summary>
    /// Test entity for data operations
    /// </summary>
    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}