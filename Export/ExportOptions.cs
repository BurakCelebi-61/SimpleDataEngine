namespace SimpleDataEngine.Export
{
    /// <summary>
    /// Export operation options
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// Export format
        /// </summary>
        public ExportFormat Format { get; set; } = ExportFormat.Json;

        /// <summary>
        /// Export scope
        /// </summary>
        public ExportScope Scope { get; set; } = ExportScope.AllData;

        /// <summary>
        /// Output file path
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Whether to include metadata
        /// </summary>
        public bool IncludeMetadata { get; set; } = true;

        /// <summary>
        /// Whether to include timestamps
        /// </summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>
        /// Whether to include audit information
        /// </summary>
        public bool IncludeAuditInfo { get; set; } = false;

        /// <summary>
        /// Whether to pretty format the output
        /// </summary>
        public bool PrettyFormat { get; set; } = true;

        /// <summary>
        /// Compression to apply to export
        /// </summary>
        public ExportCompression Compression { get; set; } = ExportCompression.None;

        /// <summary>
        /// Entity types to export (null = all)
        /// </summary>
        public List<Type> EntityTypes { get; set; }

        /// <summary>
        /// Custom filter expressions
        /// </summary>
        public Dictionary<Type, object> Filters { get; set; } = new();

        /// <summary>
        /// Maximum records per entity type (0 = no limit)
        /// </summary>
        public int MaxRecords { get; set; } = 0;

        /// <summary>
        /// Whether to validate export after creation
        /// </summary>
        public bool ValidateAfterExport { get; set; } = true;

        /// <summary>
        /// Progress callback for large exports
        /// </summary>
        public Action<ExportProgress> ProgressCallback { get; set; }

        /// <summary>
        /// Custom export description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Date range filter
        /// </summary>
        public DateRange DateRange { get; set; }
    }
}