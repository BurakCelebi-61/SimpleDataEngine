// Security/EncryptionConfig.cs
namespace SimpleDataEngine.Security
{
    /// <summary>
    /// File handler interface for database operations
    /// </summary>
    public interface IFileHandler
    {
        Task<string> ReadAllTextAsync(string path);
        Task WriteAllTextAsync(string path, string content);
        Task<byte[]> ReadAllBytesAsync(string path);
        Task WriteAllBytesAsync(string path, byte[] content);
        Task<bool> ExistsAsync(string path);
        Task DeleteAsync(string path);
        Task CopyAsync(string sourcePath, string destinationPath);
        Task MoveAsync(string sourcePath, string destinationPath);
        Task CreateDirectoryAsync(string path);
        Task<FileInfo> GetFileInfoAsync(string path);
        Task<DirectoryInfo> GetDirectoryInfoAsync(string path);
        Task<string[]> GetFilesAsync(string path, string searchPattern = "*");
        Task<string[]> GetDirectoriesAsync(string path);
    }
}
