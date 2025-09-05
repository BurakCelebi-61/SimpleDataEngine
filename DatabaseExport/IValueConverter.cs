namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Interface for value converters in database export
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Converts a value for database export
        /// </summary>
        /// <param name="value">Source value</param>
        /// <returns>Converted value</returns>
        object Convert(object value);

        /// <summary>
        /// Converts a value back from database format
        /// </summary>
        /// <param name="value">Database value</param>
        /// <returns>Original value</returns>
        object ConvertBack(object value);

        /// <summary>
        /// Gets the target data type for this converter
        /// </summary>
        string TargetDataType { get; }

        /// <summary>
        /// Checks if the converter can handle the given type
        /// </summary>
        /// <param name="sourceType">Source type to check</param>
        /// <returns>True if converter can handle the type</returns>
        bool CanConvert(Type sourceType);
    }

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