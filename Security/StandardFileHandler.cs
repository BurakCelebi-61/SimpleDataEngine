// Security/EncryptionConfig.cs
namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Standard file handler without encryption
    /// </summary>
    /// <summary>
    /// Standard file handler implementation
    /// </summary>
    public class StandardFileHandler : IFileHandler
    {
        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        public async Task WriteAllTextAsync(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(path, content);
        }

        public async Task<byte[]> ReadAllBytesAsync(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }

        public async Task WriteAllBytesAsync(string path, byte[] content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllBytesAsync(path, content);
        }

        public async Task<bool> ExistsAsync(string path)
        {
            return await Task.FromResult(File.Exists(path));
        }

        public async Task DeleteAsync(string path)
        {
            await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            });
        }

        public async Task CopyAsync(string sourcePath, string destinationPath)
        {
            await Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Copy(sourcePath, destinationPath, true);
            });
        }

        public async Task MoveAsync(string sourcePath, string destinationPath)
        {
            await Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Move(sourcePath, destinationPath);
            });
        }

        public async Task CreateDirectoryAsync(string path)
        {
            await Task.Run(() => Directory.CreateDirectory(path));
        }

        public async Task<FileInfo> GetFileInfoAsync(string path)
        {
            return await Task.FromResult(new FileInfo(path));
        }

        public async Task<DirectoryInfo> GetDirectoryInfoAsync(string path)
        {
            return await Task.FromResult(new DirectoryInfo(path));
        }

        public async Task<string[]> GetFilesAsync(string path, string searchPattern = "*")
        {
            return await Task.FromResult(Directory.GetFiles(path, searchPattern));
        }

        public async Task<string[]> GetDirectoriesAsync(string path)
        {
            return await Task.FromResult(Directory.GetDirectories(path));
        }
    }
}
