using System.Text.Json.Serialization;

namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Index entry for hierarchical storage - Path.Combine hatası düzeltildi
    /// </summary>
    public class IndexEntry
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Entity type name
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Segment identifier where entity is stored
        /// </summary>
        public string SegmentId { get; set; } = string.Empty;

        /// <summary>
        /// Offset within the segment
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Size of the entity data in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Entity version for optimistic concurrency
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Hash of the entity data for integrity checking
        /// </summary>
        public string? DataHash { get; set; }

        /// <summary>
        /// Indicates if the entity is compressed
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Indicates if the entity is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Original size before compression (if compressed)
        /// </summary>
        public int? OriginalSize { get; set; }

        /// <summary>
        /// Additional metadata as key-value pairs
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Index for searching and querying
        /// </summary>
        public Dictionary<string, object> IndexedFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Tags for categorization and filtering
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Indicates if the entry is marked for deletion (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Deletion timestamp (if deleted)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Calculate full file path for the segment - Path.Combine hatası düzeltildi
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <returns>Full file path</returns>
        public string GetSegmentFilePath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

            if (string.IsNullOrEmpty(SegmentId))
                throw new InvalidOperationException("SegmentId cannot be null or empty");

            if (string.IsNullOrEmpty(EntityType))
                throw new InvalidOperationException("EntityType cannot be null or empty");

            // DÜZELTME: string.Combine değil Path.Combine kullanılmalı
            return Path.Combine(basePath, "entities", EntityType, "segments", $"{SegmentId}.dat");
        }

        /// <summary>
        /// Get index file path
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <returns>Index file path</returns>
        public string GetIndexFilePath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

            if (string.IsNullOrEmpty(EntityType))
                throw new InvalidOperationException("EntityType cannot be null or empty");

            return Path.Combine(basePath, "entities", EntityType, "index.json");
        }

        /// <summary>
        /// Get metadata file path
        /// </summary>
        /// <param name="basePath">Base directory path</param>
        /// <returns>Metadata file path</returns>
        public string GetMetadataFilePath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

            if (string.IsNullOrEmpty(EntityType))
                throw new InvalidOperationException("EntityType cannot be null or empty");

            return Path.Combine(basePath, "entities", EntityType, "metadata.json");
        }

        /// <summary>
        /// Validate the index entry
        /// </summary>
        /// <returns>Validation result</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(Id))
                result.AddError("Id cannot be null or empty");

            if (string.IsNullOrEmpty(EntityType))
                result.AddError("EntityType cannot be null or empty");

            if (string.IsNullOrEmpty(SegmentId))
                result.AddError("SegmentId cannot be null or empty");

            if (Offset < 0)
                result.AddError("Offset cannot be negative");

            if (Size <= 0)
                result.AddError("Size must be positive");

            if (Version <= 0)
                result.AddError("Version must be positive");

            if (IsCompressed && (!OriginalSize.HasValue || OriginalSize.Value <= 0))
                result.AddError("OriginalSize must be specified and positive when IsCompressed is true");

            if (IsDeleted && !DeletedAt.HasValue)
                result.AddError("DeletedAt must be specified when IsDeleted is true");

            return result;
        }

        /// <summary>
        /// Clone the index entry
        /// </summary>
        /// <returns>Cloned index entry</returns>
        public IndexEntry Clone()
        {
            return new IndexEntry
            {
                Id = Id,
                EntityType = EntityType,
                SegmentId = SegmentId,
                Offset = Offset,
                Size = Size,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                Version = Version,
                DataHash = DataHash,
                IsCompressed = IsCompressed,
                IsEncrypted = IsEncrypted,
                OriginalSize = OriginalSize,
                Metadata = new Dictionary<string, object>(Metadata),
                IndexedFields = new Dictionary<string, object>(IndexedFields),
                Tags = new List<string>(Tags),
                IsDeleted = IsDeleted,
                DeletedAt = DeletedAt
            };
        }

        /// <summary>
        /// Update modification timestamp and version
        /// </summary>
        public void MarkAsModified()
        {
            ModifiedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// Mark entry as deleted (soft delete)
        /// </summary>
        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        /// <summary>
        /// Restore deleted entry
        /// </summary>
        public void RestoreDeleted()
        {
            IsDeleted = false;
            DeletedAt = null;
            MarkAsModified();
        }

        /// <summary>
        /// Add or update metadata
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        public void SetMetadata(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            Metadata[key] = value;
            MarkAsModified();
        }

        /// <summary>
        /// Get metadata value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Metadata key</param>
        /// <returns>Metadata value or default</returns>
        public T? GetMetadata<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || !Metadata.ContainsKey(key))
                return default(T);

            try
            {
                return (T)Metadata[key];
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Add tag
        /// </summary>
        /// <param name="tag">Tag to add</param>
        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            if (!Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                Tags.Add(tag);
                MarkAsModified();
            }
        }

        /// <summary>
        /// Remove tag
        /// </summary>
        /// <param name="tag">Tag to remove</param>
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            if (Tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                MarkAsModified();
            }
        }

        /// <summary>
        /// Check if entry has tag
        /// </summary>
        /// <param name="tag">Tag to check</param>
        /// <returns>True if tag exists</returns>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;

            return Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get formatted size string
        /// </summary>
        [JsonIgnore]
        public string FormattedSize
        {
            get
            {
                const int KB = 1024;
                const int MB = KB * 1024;

                if (Size >= MB)
                    return $"{Size / (double)MB:F2} MB";
                if (Size >= KB)
                    return $"{Size / (double)KB:F2} KB";
                return $"{Size} bytes";
            }
        }

        /// <summary>
        /// Get compression ratio (if compressed)
        /// </summary>
        [JsonIgnore]
        public double? CompressionRatio
        {
            get
            {
                if (!IsCompressed || !OriginalSize.HasValue || OriginalSize.Value == 0)
                    return null;

                return (double)Size / OriginalSize.Value;
            }
        }

        public override string ToString()
        {
            return $"IndexEntry[Id={Id}, Type={EntityType}, Segment={SegmentId}, Size={FormattedSize}, Version={Version}]";
        }

        public override bool Equals(object? obj)
        {
            if (obj is IndexEntry other)
            {
                return Id == other.Id && EntityType == other.EntityType;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, EntityType);
        }
    }
}