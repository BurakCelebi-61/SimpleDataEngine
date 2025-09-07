namespace SimpleDataEngine.Storage.Hierarchical.Models
{
    /// <summary>
    /// Entity-specific metadata
    /// </summary>
    public class EntityMetadata
    {
        /// <summary>
        /// Entity type name
        /// </summary>
        public string EntityType { get; set; }
        public string EntityName { get; set; }

        /// <summary>
        /// Current active segment number
        /// </summary>
        public int CurrentSegment { get; set; }

        /// <summary>
        /// Total records in this entity
        /// </summary>
        public long TotalRecords { get; set; }

        /// <summary>
        /// Total size in MB
        /// </summary>
        public long TotalSizeMB { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Entity creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of segments
        /// </summary>
        public List<SegmentInfo> Segments { get; set; } = new();

        /// <summary>
        /// Entity schema information
        /// </summary>
        public EntitySchema Schema { get; set; } = new();

        /// <summary>
        /// Segment information - ENHANCED WITH FileSizeBytes
        /// </summary>
        public class SegmentInfo
        {
            public string FileName { get; set; }
            public long RecordCount { get; set; }
            public long SizeMB { get; set; }

            /// <summary>
            /// EKSIK PROPERTY - File size in bytes (HATA DÜZELTİLDİ)
            /// </summary>
            public long FileSizeBytes
            {
                get => SizeMB * 1024 * 1024;
                set => SizeMB = value / (1024 * 1024);
            }

            public IdRange IdRange { get; set; } = new();
            public string Checksum { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime LastModified { get; set; } = DateTime.UtcNow;
            public bool IsActive { get; set; } = true;
            public bool IsCompressed { get; set; }
            public double CompressionRatio { get; set; }

            /// <summary>
            /// Additional properties for compatibility
            /// </summary>
            public bool IsEncrypted { get; set; }
            public Dictionary<string, object> Statistics { get; set; } = new();
            public List<string> DeletedIds { get; set; } = new();

            /// <summary>
            /// Segment version
            /// </summary>
            public string Version { get; set; } = "1.0.0";

            /// <summary>
            /// Constructor
            /// </summary>
            public SegmentInfo()
            {
                CreatedAt = DateTime.UtcNow;
                LastModified = DateTime.UtcNow;
            }

            /// <summary>
            /// Constructor with filename
            /// </summary>
            public SegmentInfo(string fileName) : this()
            {
                FileName = fileName;
            }

            /// <summary>
            /// Update size information
            /// </summary>
            public void UpdateSize(long sizeBytes)
            {
                FileSizeBytes = sizeBytes;
                LastModified = DateTime.UtcNow;
            }

            /// <summary>
            /// Update record count
            /// </summary>
            public void UpdateRecordCount(long count)
            {
                RecordCount = count;
                LastModified = DateTime.UtcNow;
            }

            /// <summary>
            /// Mark as inactive
            /// </summary>
            public void Deactivate()
            {
                IsActive = false;
                LastModified = DateTime.UtcNow;
            }

            /// <summary>
            /// Mark as active
            /// </summary>
            public void Activate()
            {
                IsActive = true;
                LastModified = DateTime.UtcNow;
            }

            /// <summary>
            /// Get size in different units
            /// </summary>
            public double GetSizeInKB() => FileSizeBytes / 1024.0;
            public double GetSizeInMB() => FileSizeBytes / (1024.0 * 1024.0);
            public double GetSizeInGB() => FileSizeBytes / (1024.0 * 1024.0 * 1024.0);

            /// <summary>
            /// Get formatted size string
            /// </summary>
            public string GetFormattedSize()
            {
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";
                else if (FileSizeBytes < 1024 * 1024)
                    return $"{GetSizeInKB():F2} KB";
                else if (FileSizeBytes < 1024 * 1024 * 1024)
                    return $"{GetSizeInMB():F2} MB";
                else
                    return $"{GetSizeInGB():F2} GB";
            }

            public override string ToString()
            {
                return $"SegmentInfo[{FileName}] - {RecordCount} records, {GetFormattedSize()}";
            }
        }

        /// <summary>
        /// ID range information
        /// </summary>
        public class IdRange
        {
            public int Min { get; set; }
            public int Max { get; set; }
            public int Count => Max - Min + 1;

            /// <summary>
            /// Check if ID is in range
            /// </summary>
            public bool Contains(int id) => id >= Min && id <= Max;

            /// <summary>
            /// Expand range to include ID
            /// </summary>
            public void ExpandToInclude(int id)
            {
                if (Min == 0 && Max == 0)
                {
                    Min = Max = id;
                }
                else
                {
                    Min = Math.Min(Min, id);
                    Max = Math.Max(Max, id);
                }
            }

            public override string ToString()
            {
                return $"IdRange[{Min}-{Max}] ({Count} items)";
            }
        }

        /// <summary>
        /// Entity schema information
        /// </summary>
        public class EntitySchema
        {
            public string Version { get; set; } = "1.0.0";
            public List<PropertyInfo> Properties { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

            public class PropertyInfo
            {
                public string Name { get; set; }
                public string Type { get; set; }
                public bool IsIndexed { get; set; }
                public bool IsRequired { get; set; }
                public object DefaultValue { get; set; }
                public int MaxLength { get; set; }
                public bool IsNullable { get; set; } = true;

                public override string ToString()
                {
                    return $"Property[{Name}:{Type}]" + (IsIndexed ? " (Indexed)" : "") + (IsRequired ? " (Required)" : "");
                }
            }

            /// <summary>
            /// Add property to schema
            /// </summary>
            public void AddProperty(string name, string type, bool isIndexed = false, bool isRequired = false)
            {
                Properties.Add(new PropertyInfo
                {
                    Name = name,
                    Type = type,
                    IsIndexed = isIndexed,
                    IsRequired = isRequired
                });
                LastUpdated = DateTime.UtcNow;
            }

            /// <summary>
            /// Get property by name
            /// </summary>
            public PropertyInfo GetProperty(string name)
            {
                return Properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            /// <summary>
            /// Check if property exists
            /// </summary>
            public bool HasProperty(string name)
            {
                return GetProperty(name) != null;
            }

            public override string ToString()
            {
                return $"EntitySchema[v{Version}] - {Properties.Count} properties";
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EntityMetadata()
        {
            CreatedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with entity name
        /// </summary>
        public EntityMetadata(string entityName) : this()
        {
            EntityName = entityName;
            EntityType = entityName;
        }

        /// <summary>
        /// Add segment to metadata
        /// </summary>
        public void AddSegment(SegmentInfo segmentInfo)
        {
            if (segmentInfo != null)
            {
                Segments.Add(segmentInfo);
                RecalculateStatistics();
            }
        }

        /// <summary>
        /// Remove segment from metadata
        /// </summary>
        public bool RemoveSegment(string fileName)
        {
            var segment = Segments.FirstOrDefault(s => s.FileName == fileName);
            if (segment != null)
            {
                Segments.Remove(segment);
                RecalculateStatistics();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Recalculate statistics from segments
        /// </summary>
        public void RecalculateStatistics()
        {
            TotalRecords = Segments.Sum(s => s.RecordCount);
            TotalSizeMB = Segments.Sum(s => s.SizeMB);
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Get active segments
        /// </summary>
        public List<SegmentInfo> GetActiveSegments()
        {
            return Segments.Where(s => s.IsActive).ToList();
        }

        /// <summary>
        /// Get total file size in bytes
        /// </summary>
        public long GetTotalFileSizeBytes()
        {
            return Segments.Sum(s => s.FileSizeBytes);
        }

        public override string ToString()
        {
            return $"EntityMetadata[{EntityName}] - {TotalRecords} records, {Segments.Count} segments, {TotalSizeMB} MB";
        }
    }
}