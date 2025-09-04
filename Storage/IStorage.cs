namespace SimpleDataEngine.Storage
{
    /// <summary>
    /// Base storage interface for data operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IStorage<T> where T : class
    {
        /// <summary>
        /// Loads all items from storage
        /// </summary>
        /// <returns>List of all items</returns>
        List<T> Load();

        /// <summary>
        /// Saves items to storage
        /// </summary>
        /// <param name="items">Items to save</param>
        void Save(List<T> items);
    }
}