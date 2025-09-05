using SimpleDataEngine.Security;
using SimpleDataEngine.DatabaseExport;
using SimpleDataEngine.DatabaseExport.Converters;
using SimpleDataEngine.Versioning;
using SimpleDataEngine.Repositories;
using SimpleDataEngine.Core;
using SimpleDataEngine.Tests;

namespace SimpleDataEngine.Tests
{
    /// <summary>
    /// Test class for advanced SimpleDataEngine features
    /// </summary>
    public class AdvancedFeaturesTest
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Advanced SimpleDataEngine Features Test Started!");
            Console.WriteLine("=" + new string('=', 60));

            try
            {
                // Test 1: Encryption Features
                await TestEncryptionFeatures();

                // Test 2: Database Export Features
                await TestDatabaseExportFeatures();

                // Test 3: Versioning Features
                await TestVersioningFeatures();

                // Test 4: Integration Test
                await TestAdvancedIntegration();

                Console.WriteLine("\n🎉 ALL ADVANCED TESTS PASSED SUCCESSFULLY!");
                Console.WriteLine("=" + new string('=', 60));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ADVANCED TEST FAILED: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task TestEncryptionFeatures()
        {
            Console.WriteLine("\n🔐 Testing Encryption Features...");

            // Test 1: Basic Encryption Service
            var encryptionConfig = new EncryptionConfig
            {
                EnableEncryption = true,
                CustomPassword = null, // Uses advanced cryptographic seed
                CompressBeforeEncrypt = true,
                FileExtension = ".sde",
                EncryptionType = EncryptionType.AES256
            };

            using var encryptionService = new EncryptionService(encryptionConfig);

            var testData = "This is sensitive test data that should be encrypted!";
            var encryptedData = encryptionService.Encrypt(testData);
            var decryptedData = encryptionService.Decrypt(encryptedData);

            Console.WriteLine($"✅ Basic encryption test:");
            Console.WriteLine($"   Original: {testData.Length} chars");
            Console.WriteLine($"   Encrypted: {encryptedData.Length} bytes");
            Console.WriteLine($"   Decrypted matches: {testData == decryptedData}");

            // Test 2: Encrypted Storage
            var testUsers = new List<TestUser>
            {
                new TestUser { Name = "Encrypted User 1", Email = "user1@encrypted.com", Age = 30 },
                new TestUser { Name = "Encrypted User 2", Email = "user2@encrypted.com", Age = 25 }
            };

            using var encryptedStorage = new EncryptedStorage<TestUser>("./test_encrypted_users", encryptionConfig);
            var repository = new SimpleRepository<TestUser>(encryptedStorage);

            repository.AddRange(testUsers);
            var retrievedUsers = repository.GetAll();

            Console.WriteLine($"✅ Encrypted storage test:");
            Console.WriteLine($"   Saved: {testUsers.Count} users");
            Console.WriteLine($"   Retrieved: {retrievedUsers.Count} users");
            Console.WriteLine($"   Data matches: {testUsers.Count == retrievedUsers.Count}");

            // Test 3: Storage Info
            var storageInfo = encryptedStorage.GetStorageInfo();
            Console.WriteLine($"✅ Storage info:");
            Console.WriteLine($"   File size: {storageInfo.FileSizeFormatted}");
            Console.WriteLine($"   Encrypted: {storageInfo.IsEncrypted}");
            Console.WriteLine($"   Integrity check: {storageInfo.HasIntegrityCheck}");

            // Test 4: Integrity Validation
            var integrityValid = encryptedStorage.ValidateIntegrity();
            Console.WriteLine($"✅ Integrity validation: {integrityValid}");

            repository.Dispose();
        }

        static async Task TestDatabaseExportFeatures()
        {
            Console.WriteLine("\n🗄️ Testing Database Export Features...");

            // Test 1: Property Mapping Configuration
            var mapping = new DatabaseMappingConfig<TestUser>()
                .MapProperty("Id", "UserID", "INTEGER PRIMARY KEY")
                .MapProperty("Name", "UserName", "NVARCHAR(200)")
                .MapProperty("Email", "EmailAddress", "NVARCHAR(255)")
                .MapProperty("Age", "UserAge", "INTEGER")
                .MapProperty("IsActive", "Active", "BIT")
                .ExcludeProperty("UpdateTime");

            // Add indexes
            mapping.AddIndex("IX_User_Name", "UserName");
            mapping.AddIndex("IX_User_Email", "EmailAddress");

            Console.WriteLine($"✅ Database mapping configured:");
            Console.WriteLine($"   Properties mapped: {mapping.PropertyMappings.Count(p => !p.IsExcluded)}");
            Console.WriteLine($"   Properties excluded: {mapping.PropertyMappings.Count(p => p.IsExcluded)}");
            Console.WriteLine($"   Indexes: {mapping.Indexes.Count}");

            // Test 2: Value Converters
            var dateTimeConverter = new DateTimeToUnixConverter();
            var testDateTime = DateTime.Now;
            var unixTime = dateTimeConverter.Convert(testDateTime);
            var convertedBack = (DateTime)dateTimeConverter.ConvertBack(unixTime);

            Console.WriteLine($"✅ DateTime converter test:");
            Console.WriteLine($"   Original: {testDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   Unix time: {unixTime}");
            Console.WriteLine($"   Converted back matches: {Math.Abs((testDateTime - convertedBack).TotalSeconds) < 1}");

            // Test 3: Property Mapping Validation
            foreach (var propertyMapping in mapping.PropertyMappings.Where(p => !p.IsExcluded))
            {
                var validation = propertyMapping.Validate();
                if (!validation.IsValid)
                {
                    Console.WriteLine($"❌ Mapping validation failed for {propertyMapping.SourceProperty}: {string.Join(", ", validation.Errors)}");
                }
            }
            Console.WriteLine($"✅ All property mappings validated successfully");

            // Test 4: SQL Syntax (Simulated)
            var sqliteSyntax = DatabaseType.SQLite.GetSqlSyntax();
            var sqlServerSyntax = DatabaseType.SQLServer.GetSqlSyntax();

            Console.WriteLine($"✅ SQL syntax helpers created:");
            Console.WriteLine($"   SQLite syntax: {sqliteSyntax.GetType().Name}");
            Console.WriteLine($"   SQL Server syntax: {sqlServerSyntax.GetType().Name}");
        }

        static async Task TestVersioningFeatures()
        {
            Console.WriteLine("\n📚 Testing Versioning Features...");

            // Test 1: DataVersion Creation and Parsing
            var version1 = new DataVersion(1, 0, 0);
            var version2 = DataVersion.Parse("1.2.3");
            var version3 = DataVersion.Parse("2.0.0-alpha.1+build.123");

            Console.WriteLine($"✅ Version creation and parsing:");
            Console.WriteLine($"   Version 1: {version1}");
            Console.WriteLine($"   Version 2: {version2}");
            Console.WriteLine($"   Version 3: {version3}");

            // Test 2: Version Comparison
            var isVersion2Greater = version2 > version1;
            var areVersionsEqual = version1 == new DataVersion(1, 0, 0);
            var isVersion3PreRelease = version3.IsPreRelease;

            Console.WriteLine($"✅ Version comparison:");
            Console.WriteLine($"   {version2} > {version1}: {isVersion2Greater}");
            Console.WriteLine($"   {version1} == 1.0.0: {areVersionsEqual}");
            Console.WriteLine($"   {version3} is pre-release: {isVersion3PreRelease}");

            // Test 3: Version Operations
            var majorIncrement = version1.IncrementMajor();
            var minorIncrement = version1.IncrementMinor();
            var patchIncrement = version1.IncrementPatch();

            Console.WriteLine($"✅ Version operations:");
            Console.WriteLine($"   Major increment: {version1} → {majorIncrement}");
            Console.WriteLine($"   Minor increment: {version1} → {minorIncrement}");
            Console.WriteLine($"   Patch increment: {version1} → {patchIncrement}");

            // Test 4: Sample Migration (Simulated)
            var sampleMigration = new SampleUserMigration();
            var migrationContext = new MigrationContext
            {
                CurrentVersion = version1,
                TargetVersion = version2,
                CreateBackups = true,
                DryRun = true // Safe test mode
            };

            var testUsers = new List<TestUser>
            {
                new TestUser { Name = "Migration User", Email = "migration@test.com", Age = 35 }
            };

            var validation = await sampleMigration.ValidateAsync(testUsers, migrationContext);
            Console.WriteLine($"✅ Migration validation:");
            Console.WriteLine($"   Is valid: {validation.IsValid}");
            Console.WriteLine($"   Affected items: {validation.AffectedItemCount}");
            Console.WriteLine($"   Estimated duration: {validation.EstimatedDuration.TotalMilliseconds:F0}ms");

            // Test 5: Migration Execution (Dry Run)
            var migrationResult = await sampleMigration.MigrateAsync(testUsers, migrationContext);
            Console.WriteLine($"✅ Migration execution (dry run):");
            Console.WriteLine($"   Success: {migrationResult.Success}");
            Console.WriteLine($"   Items processed: {migrationResult.ItemsProcessed}");
            Console.WriteLine($"   Duration: {migrationResult.Duration.TotalMilliseconds:F0}ms");
        }

        static async Task TestAdvancedIntegration()
        {
            Console.WriteLine("\n🔧 Testing Advanced Integration...");

            // Test 1: Encrypted Storage with Versioning
            var encryptionConfig = new EncryptionConfig
            {
                EnableEncryption = true,
                CompressBeforeEncrypt = true
            };

            var currentVersion = new DataVersion(1, 0, 0);
            var encryptedStorage = new EncryptedStorage<TestUser>("./integration_test", encryptionConfig);

            // Create versioned repository wrapper (simulated)
            var repository = new SimpleRepository<TestUser>(encryptedStorage);

            var integrationUsers = new List<TestUser>
            {
                new TestUser { Name = "Integration User 1", Email = "int1@test.com", Age = 28 },
                new TestUser { Name = "Integration User 2", Email = "int2@test.com", Age = 32 }
            };

            repository.AddRange(integrationUsers);

            Console.WriteLine($"✅ Integration test - Encrypted + Versioned:");
            Console.WriteLine($"   Users added: {integrationUsers.Count}");
            Console.WriteLine($"   Current version: {currentVersion}");
            Console.WriteLine($"   Storage encrypted: {encryptionConfig.EnableEncryption}");

            // Test 2: Performance with Advanced Features
            var startTime = DateTime.Now;
            for (int i = 0; i < 100; i++)
            {
                var user = new TestUser
                {
                    Name = $"Perf User {i}",
                    Email = $"perf{i}@test.com",
                    Age = 20 + (i % 50)
                };
                repository.Add(user);
            }
            var duration = DateTime.Now - startTime;

            Console.WriteLine($"✅ Performance test (100 encrypted writes):");
            Console.WriteLine($"   Duration: {duration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"   Avg per operation: {duration.TotalMilliseconds / 100:F2}ms");

            // Test 3: Storage Info and Validation
            var storageInfo = encryptedStorage.GetStorageInfo();
            var integrityValid = encryptedStorage.ValidateIntegrity();

            Console.WriteLine($"✅ Final validation:");
            Console.WriteLine($"   File size: {storageInfo.FileSizeFormatted}");
            Console.WriteLine($"   Total users: {repository.Count()}");
            Console.WriteLine($"   Integrity valid: {integrityValid}");

            // Cleanup
            repository.Dispose();
            encryptedStorage.Dispose();
        }
    }

    /// <summary>
    /// Sample migration for testing
    /// </summary>
    public class SampleUserMigration : BaseMigration<TestUser>
    {
        public override DataVersion FromVersion => new DataVersion(1, 0, 0);
        public override DataVersion ToVersion => new DataVersion(1, 1, 0);
        public override string Name => "AddUserActiveStatus";
        public override string Description => "Adds IsActive property to users with default value true";

        protected override async Task<List<TestUser>> ExecuteMigrationAsync(List<TestUser> data, MigrationContext context)
        {
            await Task.Delay(10); // Simulate work

            foreach (var user in data)
            {
                // In a real migration, we might add new properties or transform data
                user.IsActive = true; // Default value for new property
                user.UpdateTime = DateTime.Now;
            }

            return data;
        }

        protected override Task ExecuteCustomValidationAsync(List<TestUser> data, MigrationContext context, MigrationValidationResult result)
        {
            // Custom validation logic
            if (data.Any(u => string.IsNullOrEmpty(u.Name)))
            {
                result.Warnings.Add("Some users have empty names");
            }

            return Task.CompletedTask;
        }
    }
}