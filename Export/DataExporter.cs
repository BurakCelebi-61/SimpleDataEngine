using SimpleDataEngine.Audit;
using SimpleDataEngine.Core;
using System.Text;
using System.Text.Json;

namespace SimpleDataEngine.Export
{
    /// <summary>
    /// Manages data export operations for SimpleDataEngine
    /// </summary>
    public static class DataExporter
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Exports data according to the specified options
        /// </summary>
        /// <param name="options">Export options</param>
        /// <returns>Export result</returns>
        public static ExportResult Export(ExportOptions options)
        {
            lock (_lock)
            {
                var startTime = DateTime.Now;
                var result = new ExportResult();

                try
                {
                    ValidateExportOptions(options);

                    var progress = new ExportProgress
                    {
                        CurrentOperation = "Initializing export"
                    };
                    options.ProgressCallback?.Invoke(progress);

                    // Collect data
                    var exportData = CollectExportData(options, progress);

                    // Generate output
                    var outputPath = GenerateOutput(exportData, options, progress);

                    // Apply compression if requested
                    if (options.Compression != ExportCompression.None)
                    {
                        outputPath = CompressOutput(outputPath, options.Compression, progress);
                    }

                    // Validate export if requested
                    if (options.ValidateAfterExport)
                    {
                        ValidateExport(outputPath, options, progress);
                    }

                    // Build result
                    var fileInfo = new FileInfo(outputPath);
                    result.Success = true;
                    result.ExportPath = outputPath;
                    result.FileSizeBytes = fileInfo.Length;
                    result.TotalRecords = exportData.Sum(kvp => kvp.Value.Count);
                    result.RecordCounts = exportData.ToDictionary(
                        kvp => kvp.Key.Name,
                        kvp => kvp.Value.Count);
                    result.Duration = DateTime.Now - startTime;

                    AuditLogger.Log("DATA_EXPORTED", new
                    {
                        options.Format,
                        ExportPath = outputPath,
                        RecordCount = result.TotalRecords,
                        result.Duration,
                        FileSize = result.FileSizeBytes
                    });

                    return result;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.Duration = DateTime.Now - startTime;

                    AuditLogger.LogError("DATA_EXPORT_FAILED", ex);
                    return result;
                }
            }
        }

        /// <summary>
        /// Exports data asynchronously
        /// </summary>
        /// <param name="options">Export options</param>
        /// <returns>Task with export result</returns>
        public static async Task<ExportResult> ExportAsync(ExportOptions options)
        {
            return await Task.Run(() => Export(options));
        }

        /// <summary>
        /// Exports a single entity type
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="outputPath">Output file path</param>
        /// <param name="format">Export format</param>
        /// <returns>Export result</returns>
        public static ExportResult ExportEntity<T>(string outputPath, ExportFormat format = ExportFormat.Json)
            where T : class, IEntity
        {
            var options = new ExportOptions
            {
                Format = format,
                OutputPath = outputPath,
                Scope = ExportScope.SingleEntity,
                EntityTypes = new List<Type> { typeof(T) }
            };

            return Export(options);
        }

        /// <summary>
        /// Creates a quick JSON export of all data
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <returns>Export result</returns>
        public static ExportResult QuickJsonExport(string outputPath)
        {
            var options = new ExportOptions
            {
                Format = ExportFormat.Json,
                OutputPath = outputPath,
                Scope = ExportScope.AllData,
                PrettyFormat = true,
                IncludeMetadata = true,
                ValidateAfterExport = false
            };

            return Export(options);
        }

        /// <summary>
        /// Creates a CSV export for reporting
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <param name="entityTypes">Entity types to export</param>
        /// <returns>Export result</returns>
        public static ExportResult CreateReportExport(string outputPath, params Type[] entityTypes)
        {
            var options = new ExportOptions
            {
                Format = ExportFormat.Csv,
                OutputPath = outputPath,
                Scope = ExportScope.FilteredData,
                EntityTypes = entityTypes?.ToList(),
                PrettyFormat = true,
                IncludeTimestamps = true,
                Compression = ExportCompression.Zip
            };

            return Export(options);
        }

        #region Private Methods

        private static void ValidateExportOptions(ExportOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                throw new ArgumentException("Output path is required");
            }

            var directory = Path.GetDirectoryName(options.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (options.Scope == ExportScope.SingleEntity &&
                (options.EntityTypes == null || !options.EntityTypes.Any()))
            {
                throw new ArgumentException("Entity types must be specified for single entity export");
            }
        }

        private static Dictionary<Type, List<object>> CollectExportData(ExportOptions options, ExportProgress progress)
        {
            progress.CurrentOperation = "Collecting data";
            var exportData = new Dictionary<Type, List<object>>();

            // Get entity types to export
            var entityTypes = options.EntityTypes ?? GetAllEntityTypes();

            var totalTypes = entityTypes.Count;
            var processedTypes = 0;

            foreach (var entityType in entityTypes)
            {
                progress.CurrentEntityType = entityType.Name;
                progress.CurrentOperation = $"Collecting {entityType.Name} data";

                var data = CollectEntityData(entityType, options);
                if (data.Any())
                {
                    exportData[entityType] = data;
                }

                processedTypes++;
                progress.ProcessedRecords = processedTypes;
                progress.TotalRecords = totalTypes;
                options.ProgressCallback?.Invoke(progress);
            }

            return exportData;
        }

        private static List<object> CollectEntityData(Type entityType, ExportOptions options)
        {
            // This would integrate with your storage system
            // For now, returning empty list - implement based on your storage layer
            return new List<object>();
        }

        private static List<Type> GetAllEntityTypes()
        {
            // This would scan for all entity types in your system
            // For now, returning empty list - implement based on your entity registration
            return new List<Type>();
        }

        private static string GenerateOutput(Dictionary<Type, List<object>> exportData, ExportOptions options, ExportProgress progress)
        {
            progress.CurrentOperation = "Generating output";

            return options.Format switch
            {
                ExportFormat.Json => GenerateJsonOutput(exportData, options),
                ExportFormat.Csv => GenerateCsvOutput(exportData, options),
                ExportFormat.Xml => GenerateXmlOutput(exportData, options),
                ExportFormat.Excel => GenerateExcelOutput(exportData, options),
                ExportFormat.Sql => GenerateSqlOutput(exportData, options),
                ExportFormat.Binary => GenerateBinaryOutput(exportData, options),
                _ => throw new NotSupportedException($"Export format {options.Format} is not supported")
            };
        }

        private static string GenerateJsonOutput(Dictionary<Type, List<object>> exportData, ExportOptions options)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = options.PrettyFormat,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var outputData = new Dictionary<string, object>();

            // Add metadata if requested
            if (options.IncludeMetadata)
            {
                outputData["metadata"] = new
                {
                    exportedAt = DateTime.Now,
                    exportFormat = options.Format.ToString(),
                    description = options.Description,
                    totalRecords = exportData.Sum(kvp => kvp.Value.Count),
                    entityTypes = exportData.Keys.Select(t => t.Name).ToArray(),
                    version = "1.0"
                };
            }

            // Add entity data
            foreach (var kvp in exportData)
            {
                var entityName = kvp.Key.Name.ToLower();
                outputData[entityName] = kvp.Value;
            }

            var json = JsonSerializer.Serialize(outputData, jsonOptions);
            File.WriteAllText(options.OutputPath, json, Encoding.UTF8);

            return options.OutputPath;
        }

        private static string GenerateCsvOutput(Dictionary<Type, List<object>> exportData, ExportOptions options)
        {
            var directory = Path.GetDirectoryName(options.OutputPath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(options.OutputPath);

            foreach (var kvp in exportData)
            {
                var entityType = kvp.Key;
                var data = kvp.Value;

                if (!data.Any()) continue;

                var csvPath = Path.Combine(directory, $"{fileNameWithoutExt}_{entityType.Name}.csv");
                GenerateCsvForEntity(data, csvPath, options);
            }

            return options.OutputPath;
        }

        private static void GenerateCsvForEntity(List<object> data, string csvPath, ExportOptions options)
        {
            if (!data.Any()) return;

            var properties = data.First().GetType().GetProperties()
                .Where(p => p.CanRead && IsSerializableType(p.PropertyType))
                .ToArray();

            using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);

            // Write headers
            var headers = properties.Select(p => p.Name);
            if (options.IncludeTimestamps)
            {
                headers = headers.Concat(new[] { "ExportedAt" });
            }
            writer.WriteLine(string.Join(",", headers.Select(EscapeCsvValue)));

            // Write data
            foreach (var item in data)
            {
                var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "");
                if (options.IncludeTimestamps)
                {
                    values = values.Concat(new[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
                }
                writer.WriteLine(string.Join(",", values.Select(EscapeCsvValue)));
            }
        }

        private static string GenerateXmlOutput(Dictionary<Type, List<object>> exportData, ExportOptions options)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<SimpleDataEngineExport>");

            if (options.IncludeMetadata)
            {
                xml.AppendLine("  <Metadata>");
                xml.AppendLine($"    <ExportedAt>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</ExportedAt>");
                xml.AppendLine($"    <Format>{options.Format}</Format>");
                xml.AppendLine($"    <TotalRecords>{exportData.Sum(kvp => kvp.Value.Count)}</TotalRecords>");
                if (!string.IsNullOrEmpty(options.Description))
                {
                    xml.AppendLine($"    <Description>{EscapeXml(options.Description)}</Description>");
                }
                xml.AppendLine("  </Metadata>");
            }

            foreach (var kvp in exportData)
            {
                var entityName = kvp.Key.Name;
                xml.AppendLine($"  <{entityName}Collection>");

                foreach (var item in kvp.Value)
                {
                    xml.AppendLine($"    <{entityName}>");
                    SerializeObjectToXml(item, xml, "      ");
                    xml.AppendLine($"    </{entityName}>");
                }

                xml.AppendLine($"  </{entityName}Collection>");
            }

            xml.AppendLine("</SimpleDataEngineExport>");

            File.WriteAllText(options.OutputPath, xml.ToString(), Encoding.UTF8);
            return options.OutputPath;
        }

        private static string GenerateExcelOutput(Dictionary<Type, List<object>> exportData, ExportOptions options)
        {
            // This would require a library like EPPlus or ClosedXML
            // For now, fall back to CSV
            return GenerateCsvOutput(exportData, options);
        }

        private static string GenerateSqlOutput(Dictionary<Type, List<object>> exportData, ExportOptions options)
        {
            var sql = new StringBuilder();

            if (options.IncludeMetadata)
            {
                sql.AppendLine("-- SimpleDataEngine Export");
                sql.AppendLine($"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sql.AppendLine($"-- Total records: {exportData.Sum(kvp => kvp.Value.Count)}");
                sql.AppendLine();
            }

            foreach (var kvp in exportData)
            {
                var entityType = kvp.Key;
                var data = kvp.Value;
                var tableName = entityType.Name;

                if (!data.Any()) continue;

                // Generate CREATE TABLE statement
                sql.AppendLine(GenerateCreateTableStatement(entityType, tableName));
                sql.AppendLine();

                // Generate INSERT statements
                sql.AppendLine($"-- Inserting data for {tableName}");
                foreach (var item in data)
                {
                    sql.AppendLine(GenerateInsertStatement(item, tableName));
                }
                sql.AppendLine();
            }

            File.WriteAllText(options.OutputPath, sql.ToString(), Encoding.UTF8);
            return options.OutputPath;
        }

        private static string GenerateBinaryOutput(Dictionary<Type, List<object>> exportData, ExportOptions options)
        {
            // Simple binary serialization - in production, consider using more robust formats
            using var stream = new FileStream(options.OutputPath, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            // Write header
            writer.Write("SDE1"); // Magic number + version
            writer.Write(DateTime.Now.ToBinary());
            writer.Write(exportData.Count);

            foreach (var kvp in exportData)
            {
                writer.Write(kvp.Key.Name);
                writer.Write(kvp.Value.Count);

                var json = JsonSerializer.Serialize(kvp.Value);
                writer.Write(json);
            }

            return options.OutputPath;
        }

        private static string CompressOutput(string outputPath, ExportCompression compression, ExportProgress progress)
        {
            progress.CurrentOperation = "Compressing output";

            var compressedPath = compression switch
            {
                ExportCompression.Zip => outputPath + ".zip",
                ExportCompression.GZip => outputPath + ".gz",
                _ => outputPath
            };

            switch (compression)
            {
                case ExportCompression.Zip:
                    System.IO.Compression.ZipFile.CreateFromDirectory(
                        Path.GetDirectoryName(outputPath),
                        compressedPath,
                        System.IO.Compression.CompressionLevel.Optimal,
                        false);
                    break;

                case ExportCompression.GZip:
                    using (var originalFileStream = new FileStream(outputPath, FileMode.Open))
                    using (var compressedFileStream = new FileStream(compressedPath, FileMode.Create))
                    using (var compressionStream = new System.IO.Compression.GZipStream(compressedFileStream, System.IO.Compression.CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }
                    break;
            }

            // Delete original if compression succeeded
            if (File.Exists(compressedPath))
            {
                File.Delete(outputPath);
                return compressedPath;
            }

            return outputPath;
        }

        private static void ValidateExport(string exportPath, ExportOptions options, ExportProgress progress)
        {
            progress.CurrentOperation = "Validating export";

            if (!File.Exists(exportPath))
            {
                throw new FileNotFoundException($"Export file was not created: {exportPath}");
            }

            var fileInfo = new FileInfo(exportPath);
            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException("Export file is empty");
            }

            // Format-specific validation
            switch (options.Format)
            {
                case ExportFormat.Json:
                    ValidateJsonFile(exportPath);
                    break;
                case ExportFormat.Xml:
                    ValidateXmlFile(exportPath);
                    break;
            }
        }

        private static void ValidateJsonFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                JsonDocument.Parse(content);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON export: {ex.Message}");
            }
        }

        private static void ValidateXmlFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                System.Xml.XmlDocument doc = new();
                doc.LoadXml(content);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new InvalidOperationException($"Invalid XML export: {ex.Message}");
            }
        }

        private static bool IsSerializableType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(decimal) ||
                   type == typeof(Guid) ||
                   type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static void SerializeObjectToXml(object obj, StringBuilder xml, string indent)
        {
            var properties = obj.GetType().GetProperties()
                .Where(p => p.CanRead && IsSerializableType(p.PropertyType));

            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    xml.AppendLine($"{indent}<{prop.Name}>{EscapeXml(value.ToString())}</{prop.Name}>");
                }
            }
        }

        private static string GenerateCreateTableStatement(Type entityType, string tableName)
        {
            var sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE [{tableName}] (");

            var properties = entityType.GetProperties()
                .Where(p => p.CanRead && IsSerializableType(p.PropertyType))
                .ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var sqlType = GetSqlType(prop.PropertyType);
                var comma = i < properties.Length - 1 ? "," : "";

                sql.AppendLine($"    [{prop.Name}] {sqlType}{comma}");
            }

            sql.AppendLine(");");
            return sql.ToString();
        }

        private static string GenerateInsertStatement(object item, string tableName)
        {
            var properties = item.GetType().GetProperties()
                .Where(p => p.CanRead && IsSerializableType(p.PropertyType))
                .ToArray();

            var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
            var values = string.Join(", ", properties.Select(p => FormatSqlValue(p.GetValue(item))));

            return $"INSERT INTO [{tableName}] ({columns}) VALUES ({values});";
        }

        private static string GetSqlType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.Name switch
            {
                nameof(Int32) => "INT",
                nameof(Int64) => "BIGINT",
                nameof(String) => "NVARCHAR(MAX)",
                nameof(DateTime) => "DATETIME2",
                nameof(Boolean) => "BIT",
                nameof(Decimal) => "DECIMAL(18,2)",
                nameof(Double) => "FLOAT",
                nameof(Guid) => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR(MAX)"
            };
        }

        private static string FormatSqlValue(object value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "1" : "0",
                _ => value.ToString()
            };
        }

        #endregion
    }
}