namespace SimpleDataEngine.DatabaseExport.Converters
{
    /// <summary>
    /// Converts DateTime to Unix timestamp in milliseconds
    /// </summary>
    public class DateTimeToUnixMillisConverter : ValueConverterBase
    {
        public override string TargetDataType => "BIGINT";

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
                DateTime dateTime => ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds(),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToUnixTimeMilliseconds(),
                null => null,
                _ => throw new ArgumentException($"Cannot convert {value.GetType().Name} to Unix timestamp")
            };
        }

        public override object ConvertBack(object value)
        {
            return value switch
            {
                long unixTime => DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime,
                null => null,
                _ => throw new ArgumentException($"Cannot convert {value.GetType().Name} from Unix timestamp")
            };
        }
    }
}