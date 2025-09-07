namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Segment operation result
    /// </summary>
    public class SegmentOperationResult
    {
        /// <summary>
        /// Whether operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Affected segment file
        /// </summary>
        public string SegmentFile { get; set; }

        /// <summary>
        /// Records affected
        /// </summary>
        public int RecordsAffected { get; set; }

        /// <summary>
        /// Operation duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Any exception that occurred
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Creates successful result
        /// </summary>
        public static SegmentOperationResult CreateSuccess(string segmentFile, int recordsAffected, TimeSpan duration)
        {
            return new SegmentOperationResult
            {
                Success = true,
                SegmentFile = segmentFile,
                RecordsAffected = recordsAffected,
                Duration = duration,
                Message = "Operation completed successfully"
            };
        }

        /// <summary>
        /// Creates failed result
        /// </summary>
        public static SegmentOperationResult CreateFailure(string message, Exception exception = null)
        {
            return new SegmentOperationResult
            {
                Success = false,
                Message = message,
                Exception = exception
            };
        }
    }
}