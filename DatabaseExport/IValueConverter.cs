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
}