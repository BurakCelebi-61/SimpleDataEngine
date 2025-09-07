namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Standard file handler for non-encrypted operations
    /// </summary>
    public class StandardFileHandler : IFileHandler
    {
        private bool _disposed = false;

        /// <inheritdoc />
        public string FileExtension => ".json";

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(string path)
        {
            ThrowIfDisposed();

            if (!File.Exists(path))
                return null;

            try
            {
                return await File.ReadAllTextAsync(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read file: {path}", ex);
            }
        }

        /// <inheritdoc />
        public async Task WriteTextAsync(string path, string content)
        {
            ThrowIfDisposed();

            try
            {
                await EnsureDirectoryAsync(Path.GetDirectoryName(path));
                await File.WriteAllTextAsync(path, content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write file: {path}", ex);
            }
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string path)
        {
            ThrowIfDisposed();
            return Task.FromResult(File.Exists(path));
        }

        /// <inheritdoc />
        public Task DeleteAsync(string path)
        {
            ThrowIfDisposed();

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete file: {path}", ex);
            }
        }

        /// <inheritdoc />
        public Task<FileInfo> GetFileInfoAsync(string path)
        {
            ThrowIfDisposed();
            return Task.FromResult(File.Exists(path) ? new FileInfo(path) : null);
        }

        /// <inheritdoc />
        public Task EnsureDirectoryAsync(string path)
        {
            ThrowIfDisposed();

            try
            {
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create directory: {path}", ex);
            }
        }

        /// <inheritdoc />
        public async Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = true)
        {
            ThrowIfDisposed();

            try
            {
                await EnsureDirectoryAsync(Path.GetDirectoryName(destinationPath));
                File.Copy(sourcePath, destinationPath, overwrite);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to copy file from {sourcePath} to {destinationPath}", ex);
            }
        }

        /// <inheritdoc />
        public async Task MoveAsync(string sourcePath, string destinationPath)
        {
            ThrowIfDisposed();

            try
            {
                await EnsureDirectoryAsync(Path.GetDirectoryName(destinationPath));
                File.Move(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to move file from {sourcePath} to {destinationPath}", ex);
            }
        }

        /// <inheritdoc />
        public Task<List<string>> GetFilesAsync(string directoryPath, string searchPattern = null)
        {
            ThrowIfDisposed();

            try
            {
                if (!Directory.Exists(directoryPath))
                    return Task.FromResult(new List<string>());

                var pattern = searchPattern ?? $"*{FileExtension}";
                var files = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly).ToList();
                return Task.FromResult(files);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get files from directory: {directoryPath}", ex);
            }
        }

        /// <inheritdoc />
        public Task<long> GetFileSizeAsync(string path)
        {
            ThrowIfDisposed();

            try
            {
                if (!File.Exists(path))
                    return Task.FromResult(0L);

                var fileInfo = new FileInfo(path);
                return Task.FromResult(fileInfo.Length);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get file size: {path}", ex);
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if handler is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}