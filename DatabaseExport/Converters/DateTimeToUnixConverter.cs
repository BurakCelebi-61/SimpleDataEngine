namespace SimpleDataEngine.DatabaseExport.Converters
{
    /// <summary>
    /// Converts DateTime to Unix timestamp for database storage
    /// </summary>
    public class DateTimeToUnixConverter : ValueConverterBase
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
                DateTime dateTime => ((DateTimeOffset)dateTime).ToUnixTimeSeconds(),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToUnixTimeSeconds(),
                null => null,
                _ => throw new ArgumentException($"Cannot convert {value.GetType().Name} to Unix timestamp")
            };
        }

        public override object ConvertBack(object value)
        {
            return value switch
            {
                long unixTime => DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime,
                int unixTime => DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime,
                null => null,
                _ => throw new ArgumentException($"Cannot convert {value.GetType().Name} from Unix timestamp")
            };
        }
    }

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