namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Category statistics
    /// </summary>
    public class CategoryStats
    {
        public int Count { get; set; }
        public double Percentage { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }
}