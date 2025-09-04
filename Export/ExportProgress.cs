namespace SimpleDataEngine.Export
{
    /// <summary>
    /// Export progress information
    /// </summary>
    public class ExportProgress
    {
        public string CurrentOperation { get; set; }
        public int ProcessedRecords { get; set; }
        public int TotalRecords { get; set; }
        public double ProgressPercentage => TotalRecords > 0 ? ProcessedRecords * 100.0 / TotalRecords : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public string CurrentEntityType { get; set; }
    }
}