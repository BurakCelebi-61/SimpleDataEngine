namespace SimpleDataEngine.Storage
{
    /// <summary>
    /// Extended storage interface with async support
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IAsyncStorage<T> : IStorage<T> where T : class
    {
        /// <summary>
        /// Asynchronously loads all items from storage
        /// </summary>
        /// <returns>List of all items</returns>
        Task<List<T>> LoadAsync();

        /// <summary>
        /// Asynchronously saves items to storage
        /// </summary>
        /// <param name="items">Items to save</param>
        Task SaveAsync(List<T> items);
    }
}