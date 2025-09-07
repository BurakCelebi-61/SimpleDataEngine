namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Base class for value converters
    /// </summary>
    public abstract class ValueConverterBase : IValueConverter
    {
        public abstract object Convert(object value);
        public abstract object ConvertBack(object value);
        public abstract string TargetDataType { get; }
        public abstract bool CanConvert(Type sourceType);

        /// <summary>
        /// Safely converts a value with error handling
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="defaultValue">Default value if conversion fails</param>
        /// <returns>Converted value or default</returns>
        protected virtual object SafeConvert(object value, object defaultValue = null)
        {
            try
            {
                return value == null ? defaultValue : Convert(value);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}