namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Migration statistics
    /// </summary>
    public class MigrationStatistics
    {
        /// <summary>
        /// Items added during migration
        /// </summary>
        public int ItemsAdded { get; set; }

        /// <summary>
        /// Items modified during migration
        /// </summary>
        public int ItemsModified { get; set; }

        /// <summary>
        /// Items removed during migration
        /// </summary>
        public int ItemsRemoved { get; set; }

        /// <summary>
        /// Items that failed to migrate
        /// </summary>
        public int ItemsFailed { get; set; }

        /// <summary>
        /// Properties added to entities
        /// </summary>
        public int PropertiesAdded { get; set; }

        /// <summary>
        /// Properties removed from entities
        /// </summary>
        public int PropertiesRemoved { get; set; }

        /// <summary>
        /// Properties renamed
        /// </summary>
        public int PropertiesRenamed { get; set; }

        /// <summary>
        /// Data transformations applied
        /// </summary>
        public int TransformationsApplied { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate
        {
            get
            {
                var total = ItemsAdded + ItemsModified + ItemsRemoved + ItemsFailed;
                return total > 0 ? (total - ItemsFailed) / (double)total * 100 : 100;
            }
        }
    }
}