# SimpleDataEngine - .NET Data Management Library

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![C#](https://img.shields.io/badge/C%23-11.0-239120.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)

> A comprehensive data management library for .NET applications with built-in repository pattern, performance monitoring, audit logging, health checks, data validation, and backup capabilities.

## 📋 Table of Contents

- [What is SimpleDataEngine?](#what-is-simpledataengine)
- [Core Features](#core-features)
- [Quick Start](#quick-start)
- [Basic Usage](#basic-usage)
- [Advanced Features](#advanced-features)
- [Configuration](#configuration)
- [Architecture](#architecture)
- [Performance](#performance)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

## What is SimpleDataEngine?

**SimpleDataEngine** is a lightweight, file-based data management solution for .NET 8.0 applications. It provides a complete data access layer with enterprise-grade features without requiring a database server.

### Key Benefits

- **Zero Configuration** - Works immediately with sensible defaults
- **File-Based Storage** - No database server required
- **Enterprise Features** - Audit logging, health checks, performance monitoring
- **Repository Pattern** - Clean, testable data access layer
- **Async Support** - Full async/await support for all operations
- **Data Validation** - Built-in and custom validation rules
- **Automatic Backups** - Scheduled and manual backup capabilities

### Comparison with Alternatives

| Feature | SimpleDataEngine | Entity Framework | LiteDB | SQLite |
|---------|------------------|------------------|--------|---------|
| **Setup Complexity** | Zero config | Moderate | Easy | Simple |
| **Performance Monitoring** | Built-in | Manual | Manual | Manual |
| **Audit Logging** | Built-in | Custom | Custom | Custom |
| **Health Checks** | Built-in | Custom | Manual | Manual |
| **Backup System** | Automatic | Manual | Manual | Manual |
| **File Size** | < 1MB | > 50MB | ~1MB | ~2MB |

## Core Features

### 🗄️ Data Access & Repository Pattern
- Generic repository with CRUD operations
- LINQ query support with Expression Trees
- Async/await support for all operations
- Bulk operations for high performance
- Transaction-like operations with automatic rollback

### 📊 Performance Monitoring
- Real-time operation tracking
- Performance reports with detailed statistics
- Slow operation detection
- Memory usage monitoring
- Operation success/failure rates

### 🏥 Health Monitoring
- Comprehensive health checks for all components
- Storage health monitoring with disk space checks
- Memory and system resource monitoring
- Configuration validation
- Data integrity verification

### 📝 Audit Logging
- Complete audit trail for all operations
- Searchable audit logs with filtering
- Multiple export formats (JSON, CSV, XML)
- Configurable log retention policies
- Security event tracking

### 💾 Backup System
- Automatic backup scheduling
- Manual backup creation with descriptions
- Backup validation and integrity checks
- Compression support for space efficiency
- Point-in-time recovery capabilities

### ✅ Data Validation
- Built-in validation rules (required, range, length)
- Custom validation rules with business logic
- Data annotation support
- Bulk validation for large datasets
- Validation performance tracking

### 📤 Data Export
- Multiple export formats (JSON, CSV, XML, SQL, Excel)
- Compressed export options
- Filtered exports with date ranges
- Progress reporting for large exports
- Export validation

### 🔔 Notification System
- Multi-channel notifications (console, file, email)
- Flexible subscription system
- Severity-based notification routing
- Notification history and analytics
- Custom notification handlers

### 🔐 Security Features
- Data encryption with AES-256
- Configurable encryption settings
- Data compression before encryption
- Integrity verification
- Secure key management

## Quick Start

### Installation

```bash
# Install via NuGet Package Manager
dotnet add package SimpleDataEngine

# Or via Package Manager Console
Install-Package SimpleDataEngine
```

### Define Your Data Model

```csharp
using SimpleDataEngine.Core;

public class Product : IEntity
{
    public int Id { get; set; }
    public DateTime UpdateTime { get; set; }
    
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### Create Repository and Perform Operations

```csharp
using SimpleDataEngine.Repositories;
using SimpleDataEngine.Storage;

// Initialize repository
var storage = new SimpleStorage<Product>();
var repository = new SimpleRepository<Product>(storage);

// CREATE
var product = new Product 
{ 
    Name = "Laptop", 
    Price = 999.99m, 
    Category = "Electronics",
    StockQuantity = 50
};

repository.Add(product);
Console.WriteLine($"Product created with ID: {product.Id}");

// READ
var expensiveProducts = repository.Find(p => p.Price > 500);
var electronicsProducts = repository.Find(p => p.Category == "Electronics");

// UPDATE
product.Price = 899.99m;
repository.Update(product);

// DELETE
repository.Delete(product.Id);

// BULK OPERATIONS
var newProducts = new List<Product>
{
    new Product { Name = "Mouse", Price = 29.99m, Category = "Accessories" },
    new Product { Name = "Keyboard", Price = 79.99m, Category = "Accessories" }
};

repository.AddRange(newProducts);
```

## Basic Usage

### Async Operations

```csharp
// Async CRUD operations
await repository.AddAsync(product);
var allProducts = await repository.GetAllAsync();
await repository.UpdateAsync(product);
var deletedCount = await repository.DeleteAsync(product.Id);

// Async queries
var premiumProducts = await repository.FindAsync(p => 
    p.Price > 1000 && p.Category == "Electronics");

// Async bulk operations
await repository.AddRangeAsync(newProducts);
var updatedCount = await repository.UpdateRangeAsync(products);
```

### Query Operations

```csharp
// Basic queries
var allProducts = repository.GetAll();
var productById = repository.GetById(1);

// Complex queries
var filteredProducts = repository.Find(p => 
    p.Price > 100 && 
    p.Category == "Electronics" && 
    p.StockQuantity > 0);

// Count and existence checks
var totalCount = repository.Count();
var expensiveCount = repository.Count(p => p.Price > 500);
var hasProducts = repository.Exists();
var hasExpensive = repository.Exists(p => p.Price > 1000);
```

## Advanced Features

### Performance Monitoring

```csharp
using SimpleDataEngine.Performance;

// Track operations automatically
using (var timer = PerformanceTracker.StartOperation("DatabaseQuery", "Data"))
{
    var results = repository.Find(p => p.Category == "Electronics");
}

// Generate performance reports
var report = PerformanceReportGenerator.GenerateReport(TimeSpan.FromHours(24));
Console.WriteLine($"Total Operations: {report.TotalOperations}");
Console.WriteLine($"Average Response Time: {report.AverageOperationTimeMs:F2}ms");
Console.WriteLine($"Success Rate: {report.OverallSuccessRate:F2}%");
```

### Health Monitoring

```csharp
using SimpleDataEngine.Health;

// Comprehensive health check
var healthReport = await HealthChecker.CheckHealthAsync();
Console.WriteLine($"System Health: {healthReport.OverallStatus}");
Console.WriteLine($"Healthy: {healthReport.HealthyCount}");
Console.WriteLine($"Warnings: {healthReport.WarningCount}");

// Quick health check
var isHealthy = await HealthChecker.IsHealthyAsync();
```

### Audit Logging

```csharp
using SimpleDataEngine.Audit;

// Basic audit logging
AuditLogger.Log("PRODUCT_CREATED", new { ProductId = product.Id, Name = product.Name });
AuditLogger.LogWarning("LOW_STOCK", new { ProductId = product.Id });
AuditLogger.LogError("OPERATION_FAILED", exception);

// Scoped logging
using (var scope = AuditLogger.BeginScope("ORDER_PROCESSING"))
{
    AuditLogger.Log("VALIDATION_STARTED", new { OrderId = order.Id });
    // Business logic
    AuditLogger.Log("ORDER_COMPLETED", new { OrderId = order.Id });
}

// Query audit logs
var auditQuery = new AuditQueryOptions
{
    StartDate = DateTime.Now.AddDays(-7),
    Categories = new[] { AuditCategory.Data },
    Take = 100
};

var auditResults = await AuditLogger.QueryAsync(auditQuery);
```

### Data Validation

```csharp
using SimpleDataEngine.Validation;

// Register validation rules
SimpleValidator.RegisterRule<Product>(
    BuiltInValidationRules.NotNullOrEmpty<Product>(p => p.Name, "Name"));

SimpleValidator.RegisterRule<Product>(
    BuiltInValidationRules.Range<Product>(p => p.Price, "Price", 0.01m, 999999.99m));

// Validate entities
var validationResult = SimpleValidator.Validate(product);
if (!validationResult.IsValid)
{
    Console.WriteLine($"Validation failed: {validationResult.ErrorCount} errors");
    foreach (var issue in validationResult.Issues)
    {
        Console.WriteLine($"  {issue.PropertyName}: {issue.Message}");
    }
}

// Bulk validation
var bulkResult = SimpleValidator.ValidateBulk(products);
Console.WriteLine($"Valid: {bulkResult.ValidCount}, Invalid: {bulkResult.InvalidCount}");
```

### Backup System

```csharp
using SimpleDataEngine.Backup;

// Create manual backup
var backupPath = BackupManager.CreateBackup("Manual backup before update");
Console.WriteLine($"Backup created: {Path.GetFileName(backupPath)}");

// List available backups
var backups = BackupManager.GetAvailableBackups();
foreach (var backup in backups.Take(5))
{
    Console.WriteLine($"{backup.FileName} - {backup.FileSizeFormatted} - {backup.Age.Days} days old");
}

// Validate backup
var latestBackup = backups.FirstOrDefault();
if (latestBackup != null)
{
    var validation = BackupManager.ValidateBackup(latestBackup.FilePath);
    Console.WriteLine($"Backup valid: {validation.IsValid}");
}

// Restore from backup
BackupManager.RestoreBackup(latestBackup.FilePath, validateBeforeRestore: true);
```

### Data Export

```csharp
using SimpleDataEngine.Export;

// Quick JSON export
var jsonResult = DataExporter.QuickJsonExport("products_export.json");

// Advanced export with options
var exportOptions = new ExportOptions
{
    Format = ExportFormat.Csv,
    OutputPath = "products_report.csv",
    IncludeMetadata = true,
    Compression = ExportCompression.Zip,
    DateRange = new DateRange 
    { 
        StartDate = DateTime.Now.AddDays(-30),
        EndDate = DateTime.Now
    }
};

var exportResult = await DataExporter.ExportAsync(exportOptions);
Console.WriteLine($"Exported {exportResult.TotalRecords} records in {exportResult.Duration.TotalSeconds:F2}s");
```

### Notification System

```csharp
using SimpleDataEngine.Notifications;

// Initialize notifications
NotificationEngine.Initialize();

// Send notifications
await NotificationEngine.NotifyAsync("System Alert", "Low disk space detected", 
    NotificationSeverity.Warning);

// Create subscriptions
var criticalAlerts = NotificationExtensions.CreateCriticalAlertSubscription(
    NotificationChannel.Console);
NotificationEngine.Subscribe(criticalAlerts);

// Get notifications
var notifications = NotificationEngine.GetNotifications();
var stats = NotificationEngine.GetStatistics();
```

### Encryption

```csharp
using SimpleDataEngine.Security;

// Configure encryption
var encryptionConfig = new EncryptionConfig
{
    EnableEncryption = true,
    EncryptionType = EncryptionType.AES256,
    CompressBeforeEncrypt = true
};

// Use encrypted storage
var encryptedStorage = new EncryptedStorage<Product>("./encrypted_data", encryptionConfig);
var repository = new SimpleRepository<Product>(encryptedStorage);

// Operations work the same way
repository.Add(product);
var products = repository.GetAll();
```

## Configuration

### JSON Configuration File

Create `engine-settings.json` in your application directory:

```json
{
  "database": {
    "connectionString": "Database",
    "dataDirectory": "data",
    "backupDirectory": "backups",
    "autoBackupIntervalHours": 24,
    "enableAutoBackup": true
  },
  "performance": {
    "enablePerformanceTracking": true,
    "slowQueryThresholdMs": 1000
  },
  "logging": {
    "logLevel": "Info",
    "enableFileLogging": true
  },
  "backup": {
    "enabled": true,
    "maxBackups": 10,
    "compressBackups": true
  }
}
```

### Programmatic Configuration

```csharp
using SimpleDataEngine.Configuration;

// Update configuration at runtime
ConfigManager.UpdateConfiguration(config =>
{
    config.Performance.EnablePerformanceTracking = true;
    config.Database.MaxDatabaseSizeMB = 2000;
    config.Backup.MaxBackups = 20;
});

// Validate configuration
var validation = ConfigManager.Current.Validate();
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Config Error: {error}");
    }
}
```

## Architecture

SimpleDataEngine follows proven design patterns:

- **Repository Pattern** - Clean separation of data access logic
- **Factory Pattern** - Configurable storage and validation strategies  
- **Observer Pattern** - Event-driven entity change tracking
- **Strategy Pattern** - Pluggable components (storage, validation, export)
- **Facade Pattern** - Simplified APIs for complex operations

### Component Overview

```
SimpleDataEngine
├── Core/              # Entity interfaces and utilities
├── Repositories/      # Repository pattern implementation
├── Storage/           # File-based storage implementations
├── Performance/       # Performance tracking and reporting
├── Health/            # Health monitoring and diagnostics
├── Audit/             # Comprehensive audit logging
├── Validation/        # Data validation framework
├── Backup/            # Backup and restore system
├── Export/            # Multi-format data export
├── Notifications/     # Event-driven notification system
├── Security/          # Encryption and security features
└── Configuration/     # Configuration management
```

## Performance

Performance benchmarks on Intel i7-10700K, 32GB RAM, NVMe SSD:

| Operation | Records | Time | Throughput |
|-----------|---------|------|------------|
| Insert | 1,000 | 45ms | 22,222 ops/sec |
| Insert | 10,000 | 234ms | 42,735 ops/sec |
| Query (Simple) | 10,000 | 8ms | 1,250,000 ops/sec |
| Query (Complex) | 10,000 | 24ms | 416,667 ops/sec |
| Bulk Insert | 10,000 | 156ms | 64,103 ops/sec |
| Backup Creation | 50MB | 1.2s | With compression |
| JSON Export | 10,000 records | 298ms | Pretty formatted |

### Memory Usage

| Component | Baseline | Under Load | Peak |
|-----------|----------|------------|------|
| Core Engine | 2.1MB | 8.5MB | 15MB |
| Performance Tracker | 0.3MB | 1.2MB | 2.8MB |
| Audit Logger | 0.8MB | 2.1MB | 4.5MB |

## Use Cases

### Ideal For
- Small to medium business applications
- Desktop applications requiring local data storage
- Prototyping and development environments
- Applications requiring comprehensive audit trails
- Systems needing offline data capabilities
- IoT applications with local data collection

### Industry Applications
- Customer relationship management (CRM)
- Inventory management systems
- Point of sale (POS) applications
- Document management systems
- Project management tools
- Gaming save data and statistics
- Research data collection and analysis

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Clone the repository
2. Open in Visual Studio 2022 or VS Code
3. Build the solution
4. Run tests to ensure everything works

### Guidelines

- Follow existing code style and patterns
- Add tests for new features
- Update documentation for API changes
- Follow semantic versioning for releases

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2024 Burak Çelebi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

## Contact

**Burak Çelebi**
- 📧 Email: burakcelebi61@hotmail.com
- 💼 LinkedIn: [linkedin.com/in/burakcelebi61](https://linkedin.com/in/burakcelebi61)
- 🐙 GitHub: [@BurakCelebi-61](https://github.com/BurakCelebi-61)
- 🌐 Project: [https://github.com/BurakCelebi-61/SimpleDataEngine](https://github.com/BurakCelebi-61/SimpleDataEngine)

---

<div align="center">

**Star this repository if SimpleDataEngine helps your project!** ⭐

Made with ❤️ for the .NET community

</div>
