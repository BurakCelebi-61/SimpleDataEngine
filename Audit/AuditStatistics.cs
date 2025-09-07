namespace SimpleDataEngine.Audit
{
    /// <summary>
    /// Audit statistics and metrics
    /// </summary>
    public class AuditStatistics
    {
        /// <summary>
        /// When statistics were collected
        /// </summary>
        public DateTime CollectionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total number of audit entries
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Entries grouped by audit level
        /// </summary>
        public Dictionary<AuditLevel, int> EntriesByLevel { get; set; } = new Dictionary<AuditLevel, int>();

        /// <summary>
        /// Entries grouped by category
        /// </summary>
        public Dictionary<AuditCategory, int> EntriesByCategory { get; set; } = new Dictionary<AuditCategory, int>();

        /// <summary>
        /// Average entries per day (readonly calculated property)
        /// </summary>
        public double AverageEntriesPerDay { get; }

        /// <summary>
        /// Date range for statistics
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for statistics
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Most active day
        /// </summary>
        public DateTime? MostActiveDay { get; set; }

        /// <summary>
        /// Peak entries on most active day
        /// </summary>
        public int PeakDayEntries { get; set; }

        /// <summary>
        /// Error rate percentage
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Warning rate percentage
        /// </summary>
        public double WarningRate { get; set; }

        /// <summary>
        /// Constructor to calculate average entries per day
        /// </summary>
        public AuditStatistics()
        {
            var days = (EndDate - StartDate).TotalDays;
            AverageEntriesPerDay = days > 0 ? TotalEntries / days : 0;
        }

        /// <summary>
        /// Updates calculated fields
        /// </summary>
        public void RecalculateMetrics()
        {
            if (TotalEntries > 0)
            {
                var errorCount = EntriesByLevel.GetValueOrDefault(AuditLevel.Error, 0);
                var warningCount = EntriesByLevel.GetValueOrDefault(AuditLevel.Warning, 0);

                ErrorRate = (double)errorCount / TotalEntries * 100;
                WarningRate = (double)warningCount / TotalEntries * 100;
            }
        }
    }
}