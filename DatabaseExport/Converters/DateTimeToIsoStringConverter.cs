namespace SimpleDataEngine.DatabaseExport.Converters
{
    /// <summary>
    /// Converts DateTime to ISO 8601 string format
    /// </summary>
    public class DateTimeToIsoStringConverter : ValueConverterBase
    {
        public override string TargetDataType => "VARCHAR(50)";

        public override bool CanConvert(Type sourceType)
        {
            return sourceType == typeof(DateTime) ||
                   sourceType == typeof(DateTime?) ||
                   sourceType == typeof(DateTimeOffset) ||
                   sourceType == typeof(DateTimeOffset?);
        }

        public override object Convert(object value)
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToString("O"), // ISO 8601 format
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
                null => null,
                _ => throw new ArgumentException($"Cannot convert {value.GetType().Name} to ISO string")
            };
        }

        public override object ConvertBack(object value)
        {
            return value switch
            {
                string isoString when DateTime.TryParse(isoString, out var dateTime) => dateTime,
                null => null,
                _ => throw new ArgumentException($"Cannot convert {value} from ISO string")
            };
        }
    }
}