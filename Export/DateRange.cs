namespace SimpleDataEngine.Export
{
    /// <summary>
    /// Date range for filtering exports
    /// </summary>
    public class DateRange
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DateField { get; set; } = "CreatedAt";

        public bool IsValid => StartDate.HasValue || EndDate.HasValue;
    }
}