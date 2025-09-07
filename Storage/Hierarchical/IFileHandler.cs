namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// File handler interface for unified file operations
    /// </summary>
    public interface IFileHandler : IDisposable
    {
        /// <summary>
        /// File extension used by this handler
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Reads text content from file asynchronously
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>File content or null if file doesn't exist</returns>
        Task<string> ReadTextAsync(string path);

        /// <summary>
        /// Writes text content to file asynchronously
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="content">Content to write</param>
        Task WriteTextAsync(string path, string content);

        /// <summary>
        /// Checks if file exists asynchronously
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>True if file exists</returns>
        Task<bool> ExistsAsync(string path);

        /// <summary>
        /// Deletes file asynchronously
        /// </summary>
        /// <param name="path">File path</param>
        Task DeleteAsync(string path);

        /// <summary>
        /// Gets file information asynchronously
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>FileInfo or null if file doesn't exist</returns>
        Task<FileInfo> GetFileInfoAsync(string path);

        /// <summary>
        /// Ensures directory exists asynchronously
        /// </summary>
        /// <param name="path">Directory path</param>
        Task EnsureDirectoryAsync(string path);

        /// <summary>
        /// Copies file to another location asynchronously
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <param name="overwrite">Whether to overwrite if destination exists</param>
        Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = true);

        /// <summary>
        /// Moves file to another location asynchronously
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        Task MoveAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Gets all files in directory with this handler's extension
        /// </summary>
        /// <param name="directoryPath">Directory path</param>
        /// <param name="searchPattern">Search pattern (optional)</param>
        /// <returns>List of file paths</returns>
        Task<List<string>> GetFilesAsync(string directoryPath, string searchPattern = null);

        /// <summary>
        /// Gets file size in bytes
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>File size in bytes</returns>
        Task<long> GetFileSizeAsync(string path);
    }
}