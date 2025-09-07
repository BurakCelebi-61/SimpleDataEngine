using SimpleDataEngine.Audit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Entity metadata manager for entity-specific operations
    /// </summary>
    public class EntityMetadataManager : IDisposable
    {
        private readonly IFileHandler _fileHandler;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Initializes entity metadata manager
        /// </summary>
        /// <param name="fileHandler">File handler for I/O operations</param>
        public EntityMetadataManager(IFileHandler fileHandler)
        {
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Loads entity metadata from file
        /// </summary>
        /// <param name="entityPath">Entity directory path</param>
        /// <param name="entityType">Entity type name</param>
        /// <returns>Entity metadata or new instance if file doesn't exist</returns>
        public async Task<EntityMetadata> LoadAsync(string entityPath, string entityType)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(entityPath))
                throw new ArgumentException("Entity path cannot be null or empty", nameof(entityPath));

            try
            {
                await _fileHandler.EnsureDirectoryAsync(entityPath);

                var metadataPath = Path.Combine(entityPath, $"metadata{_fileHandler.FileExtension}");
                var content = await _fileHandler.ReadTextAsync(metadataPath);

                if (string.IsNullOrEmpty(content))
                {
                    var newMetadata = CreateDefaultMetadata(entityType);
                    await SaveAsync(newMetadata, entityPath);
                    return newMetadata;
                }

                var metadata = JsonSerializer.Deserialize<EntityMetadata>(content, _jsonOptions);
                if (metadata == null)
                {
                    return CreateDefaultMetadata(entityType);
                }

                // Ensure entity type is set
                if (string.IsNullOrEmpty(metadata.EntityType))
                {
                    metadata.EntityType = entityType;
                }

                return metadata;
            }
            catch (JsonException ex)
            {
                AuditLogger.LogError("ENTITY_METADATA_LOAD_JSON_ERROR", ex, new
                {
                    EntityPath = entityPath,
                    EntityType = entityType
                });
                throw new InvalidOperationException($"Failed to parse entity metadata for {entityType}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("ENTITY_METADATA_LOAD_ERROR", ex, new
                {
                    EntityPath = entityPath,
                    EntityType = entityType
                });
                throw new InvalidOperationException($"Failed to load entity metadata for {entityType}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves entity metadata to file
        /// </summary>
        /// <param name="metadata">Metadata to save</param>
        /// <param name="entityPath">Entity directory path</param>
        public async Task SaveAsync(EntityMetadata metadata, string entityPath)
        {
            ThrowIfDisposed();

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (string.IsNullOrEmpty(entityPath))
                throw new ArgumentException("Entity path cannot be null or empty", nameof(entityPath));

            lock (_lock)
            {
                try
                {
                    metadata.LastUpdated = DateTime.Now;

                    // Recalculate totals from segments
                    RecalculateTotals(metadata);

                    var content = JsonSerializer.Serialize(metadata, _jsonOptions);
                    var metadataPath = Path.Combine(entityPath, $"metadata{_fileHandler.FileExtension}");

                    // Async save
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _fileHandler.WriteTextAsync(metadataPath, content);

                            AuditLogger.Log("ENTITY_METADATA_SAVED", new
                            {
                                EntityType = metadata.EntityType,
                                EntityPath = entityPath,
                                TotalRecords = metadata.TotalRecords,
                                TotalSegments = metadata.Segments.Count,
                                TotalSizeMB = metadata.TotalSizeMB
                            }, category: AuditCategory.Data);
                        }
                        catch (Exception ex)
                        {
                            AuditLogger.LogError("ENTITY_METADATA_SAVE_ERROR", ex, new
                            {
                                EntityType = metadata.EntityType,
                                EntityPath = entityPath
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    AuditLogger.LogError("ENTITY_METADATA_SERIALIZE_ERROR", ex, new
                    {
                        EntityType = metadata.EntityType
                    });
                    throw new InvalidOperationException($"Failed to serialize entity metadata for {metadata.EntityType}: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Adds or updates segment information
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <param name="segmentInfo">Segment information to add/update</param>
        public void AddOrUpdateSegment(EntityMetadata metadata, EntityMetadata.SegmentInfo segmentInfo)
        {
            ThrowIfDisposed();

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (segmentInfo == null)
                throw new ArgumentNullException(nameof(segmentInfo));

            lock (_lock)
            {
                var existingSegment = metadata.Segments.FirstOrDefault(s => s.FileName == segmentInfo.FileName);

                if (existingSegment != null)
                {
                    // Update existing segment
                    existingSegment.RecordCount = segmentInfo.RecordCount;
                    existingSegment.SizeMB = segmentInfo.SizeMB;
                    existingSegment.IdRange = segmentInfo.IdRange;
                    existingSegment.Checksum = segmentInfo.Checksum;
                    existingSegment.LastModified = DateTime.Now;
                    existingSegment.IsActive = segmentInfo.IsActive;
                    existingSegment.IsCompressed = segmentInfo.IsCompressed;
                    existingSegment.CompressionRatio = segmentInfo.CompressionRatio;
                }
                else
                {
                    // Add new segment
                    segmentInfo.CreatedAt = DateTime.Now;
                    segmentInfo.LastModified = DateTime.Now;
                    metadata.Segments.Add(segmentInfo);

                    // Update current segment number
                    var sequenceNumber = ExtractSequenceNumber(segmentInfo.FileName);
                    if (sequenceNumber > metadata.CurrentSegment)
                    {
                        metadata.CurrentSegment = sequenceNumber;
                    }
                }

                RecalculateTotals(metadata);
            }
        }

        /// <summary>
        /// Removes segment from metadata
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <param name="segmentFileName">Segment file name to remove</param>
        /// <returns>True if segment was removed</returns>
        public bool RemoveSegment(EntityMetadata metadata, string segmentFileName)
        {
            ThrowIfDisposed();

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (string.IsNullOrEmpty(segmentFileName))
                return false;

            lock (_lock)
            {
                var segment = metadata.Segments.FirstOrDefault(s => s.FileName == segmentFileName);
                if (segment != null)
                {
                    metadata.Segments.Remove(segment);
                    RecalculateTotals(metadata);

                    AuditLogger.Log("ENTITY_SEGMENT_REMOVED", new
                    {
                        EntityType = metadata.EntityType,
                        SegmentFileName = segmentFileName,
                        RecordsRemoved = segment.RecordCount,
                        SizeRemovedMB = segment.SizeMB
                    }, category: AuditCategory.Data);

                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets next segment sequence number
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <returns>Next segment sequence number</returns>
        public int GetNextSegmentNumber(EntityMetadata metadata)
        {
            ThrowIfDisposed();

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            return metadata.CurrentSegment + 1;
        }

        /// <summary>
        /// Generates segment file name
        /// </summary>
        /// <param name="sequenceNumber">Segment sequence number</param>
        /// <param name="fileExtension">File extension</param>
        /// <returns>Segment file name</returns>
        public string GenerateSegmentFileName(int sequenceNumber, string fileExtension = null)
        {
            ThrowIfDisposed();

            var extension = fileExtension ?? _fileHandler.FileExtension;
            return $"segment_{sequenceNumber:D6}{extension}";
        }

        /// <summary>
        /// Calculates checksum for segment data
        /// </summary>
        /// <param name="data">Data to calculate checksum for</param>
        /// <returns>Checksum string</returns>
        public string CalculateChecksum(byte[] data)
        {
            ThrowIfDisposed();

            if (data == null || data.Length == 0)
                return string.Empty;

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Calculates checksum for text content
        /// </summary>
        /// <param name="content">Text content</param>
        /// <returns>Checksum string</returns>
        public string CalculateChecksum(string content)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var data = Encoding.UTF8.GetBytes(content);
            return CalculateChecksum(data);
        }

        /// <summary>
        /// Validates segment checksum
        /// </summary>
        /// <param name="content">Content to validate</param>
        /// <param name="expectedChecksum">Expected checksum</param>
        /// <returns>True if checksum matches</returns>
        public bool ValidateChecksum(string content, string expectedChecksum)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(expectedChecksum))
                return true; // No checksum to validate

            var actualChecksum = CalculateChecksum(content);
            return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets segment by file name
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <param name="fileName">Segment file name</param>
        /// <returns>Segment info or null if not found</returns>
        public EntityMetadata.SegmentInfo GetSegment(EntityMetadata metadata, string fileName)
        {
            ThrowIfDisposed();

            if (metadata == null || string.IsNullOrEmpty(fileName))
                return null;

            return metadata.Segments.FirstOrDefault(s => s.FileName == fileName);
        }

        /// <summary>
        /// Gets segments by ID range
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <param name="minId">Minimum ID</param>
        /// <param name="maxId">Maximum ID</param>
        /// <returns>List of segments that contain the ID range</returns>
        public List<EntityMetadata.SegmentInfo> GetSegmentsByIdRange(EntityMetadata metadata, int minId, int maxId)
        {
            ThrowIfDisposed();

            if (metadata == null)
                return new List<EntityMetadata.SegmentInfo>();

            return metadata.Segments
                .Where(s => s.IsActive &&
                           !(maxId < s.IdRange.Min || minId > s.IdRange.Max))
                .OrderBy(s => s.IdRange.Min)
                .ToList();
        }

        /// <summary>
        /// Gets active segments
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <returns>List of active segments</returns>
        public List<EntityMetadata.SegmentInfo> GetActiveSegments(EntityMetadata metadata)
        {
            ThrowIfDisposed();

            if (metadata == null)
                return new List<EntityMetadata.SegmentInfo>();

            return metadata.Segments
                .Where(s => s.IsActive)
                .OrderBy(s => s.IdRange.Min)
                .ToList();
        }

        /// <summary>
        /// Updates entity schema information
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        /// <param name="properties">Property information list</param>
        public void UpdateSchema(EntityMetadata metadata, List<EntityMetadata.EntitySchema.PropertyInfo> properties)
        {
            ThrowIfDisposed();

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            metadata.Schema.Properties = properties ?? new List<EntityMetadata.EntitySchema.PropertyInfo>();
            metadata.Schema.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Checks if metadata file exists
        /// </summary>
        /// <param name="entityPath">Entity directory path</param>
        /// <returns>True if metadata file exists</returns>
        public async Task<bool> ExistsAsync(string entityPath)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(entityPath))
                return false;

            var metadataPath = Path.Combine(entityPath, $"metadata{_fileHandler.FileExtension}");
            return await _fileHandler.ExistsAsync(metadataPath);
        }

        /// <summary>
        /// Creates backup of entity metadata
        /// </summary>
        /// <param name="entityPath">Entity directory path</param>
        /// <param name="backupPath">Backup file path</param>
        public async Task CreateBackupAsync(string entityPath, string backupPath)
        {
            ThrowIfDisposed();

            var metadataPath = Path.Combine(entityPath, $"metadata{_fileHandler.FileExtension}");

            if (await _fileHandler.ExistsAsync(metadataPath))
            {
                await _fileHandler.CopyAsync(metadataPath, backupPath);

                AuditLogger.Log("ENTITY_METADATA_BACKUP_CREATED", new
                {
                    OriginalPath = metadataPath,
                    BackupPath = backupPath
                }, category: AuditCategory.Backup);
            }
        }

        /// <summary>
        /// Validates entity metadata integrity
        /// </summary>
        /// <param name="metadata">Entity metadata to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateMetadata(EntityMetadata metadata)
        {
            ThrowIfDisposed();

            var result = new ValidationResult { IsValid = true };

            if (metadata == null)
            {
                result.IsValid = false;
                result.Errors.Add("Metadata is null");
                return result;
            }

            // Validate basic properties
            if (string.IsNullOrEmpty(metadata.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Entity type is missing");
            }

            // Validate segment consistency
            var calculatedRecords = metadata.Segments.Where(s => s.IsActive).Sum(s => s.RecordCount);
            if (metadata.TotalRecords != calculatedRecords)
            {
                result.Warnings.Add($"Total records mismatch: metadata={metadata.TotalRecords}, calculated={calculatedRecords}");
            }

            var calculatedSize = metadata.Segments.Where(s => s.IsActive).Sum(s => s.SizeMB);
            if (Math.Abs(metadata.TotalSizeMB - calculatedSize) > 1) // 1MB tolerance
            {
                result.Warnings.Add($"Total size mismatch: metadata={metadata.TotalSizeMB}MB, calculated={calculatedSize}MB");
            }

            // Validate ID ranges
            var sortedSegments = metadata.Segments
                .Where(s => s.IsActive)
                .OrderBy(s => s.IdRange.Min)
                .ToList();

            for (int i = 0; i < sortedSegments.Count - 1; i++)
            {
                var current = sortedSegments[i];
                var next = sortedSegments[i + 1];

                if (current.IdRange.Max >= next.IdRange.Min)
                {
                    result.Warnings.Add($"Overlapping ID ranges: {current.FileName} ({current.IdRange.Min}-{current.IdRange.Max}) and {next.FileName} ({next.IdRange.Min}-{next.IdRange.Max})");
                }
            }

            // Validate segment files
            foreach (var segment in metadata.Segments.Where(s => s.IsActive))
            {
                if (string.IsNullOrEmpty(segment.FileName))
                {
                    result.IsValid = false;
                    result.Errors.Add("Segment with empty file name found");
                }

                if (segment.IdRange.Min > segment.IdRange.Max)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Invalid ID range in segment {segment.FileName}: min={segment.IdRange.Min}, max={segment.IdRange.Max}");
                }

                if (segment.RecordCount < 0 || segment.SizeMB < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Negative values in segment {segment.FileName}");
                }
            }

            return result;
        }

        /// <summary>
        /// Recalculates total records and size from segments
        /// </summary>
        /// <param name="metadata">Entity metadata</param>
        private void RecalculateTotals(EntityMetadata metadata)
        {
            var activeSegments = metadata.Segments.Where(s => s.IsActive);
            metadata.TotalRecords = activeSegments.Sum(s => s.RecordCount);
            metadata.TotalSizeMB = activeSegments.Sum(s => s.SizeMB);
        }

        /// <summary>
        /// Extracts sequence number from segment file name
        /// </summary>
        /// <param name="fileName">Segment file name</param>
        /// <returns>Sequence number or 0 if not found</returns>
        private int ExtractSequenceNumber(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return 0;

            // Expected format: segment_000001.json or segment_000001.sde
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var parts = nameWithoutExtension.Split('_');

            if (parts.Length == 2 && parts[0] == "segment" && int.TryParse(parts[1], out int sequenceNumber))
            {
                return sequenceNumber;
            }

            return 0;
        }

        /// <summary>
        /// Creates default entity metadata
        /// </summary>
        /// <param name="entityType">Entity type name</param>
        /// <returns>Default entity metadata</returns>
        private EntityMetadata CreateDefaultMetadata(string entityType)
        {
            return new EntityMetadata
            {
                EntityType = entityType,
                CurrentSegment = 0,
                TotalRecords = 0,
                TotalSizeMB = 0,
                CreatedAt = DateTime.Now,
                LastUpdated = DateTime.Now,
                Segments = new List<EntityMetadata.SegmentInfo>(),
                Schema = new EntityMetadata.EntitySchema
                {
                    Version = "1.0.0",
                    Properties = new List<EntityMetadata.EntitySchema.PropertyInfo>(),
                    LastUpdated = DateTime.Now
                }
            };
        }

        /// <summary>
        /// Throws ObjectDisposedException if manager is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EntityMetadataManager));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public bool HasErrors => Errors.Any();
            public bool HasWarnings => Warnings.Any();
        }
    }