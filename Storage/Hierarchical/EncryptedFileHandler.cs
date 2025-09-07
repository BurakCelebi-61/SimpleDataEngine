using SimpleDataEngine.Security;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Encrypted file handler for secure operations
    /// </summary>
    public class EncryptedFileHandler : IFileHandler
    {
        private readonly EncryptionService _encryptionService;
        private bool _disposed = false;

        /// <inheritdoc />
        public string FileExtension => ".sde";

        /// <summary>
        /// Initializes encrypted file handler with encryption configuration
        /// </summary>
        /// <param name="config">Encryption configuration</param>
        public EncryptedFileHandler(EncryptionConfig config)
        {
            _encryptionService = new EncryptionService(config ?? throw new ArgumentNullException(nameof(config)));
        }

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(string path)
        {
            ThrowIfDisposed();

            if (!File.Exists(path))
                return null;

            try
            {
                var encryptedData = await File.ReadAllBytesAsync(path);
                if (encryptedData.Length == 0)
                    return null;

                return _encryptionService.Decrypt(encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read encrypted file: {path}", ex);
            }
        }

        /// <inheritdoc />
        public async Task WriteTextAsync(string path, string content)
        {
            ThrowIfDisposed();

            try
            {
                await EnsureDirectoryAsync(Path.GetDirectoryName(path));

                if (string.IsNullOrEmpty(content))
                {
                    await File.WriteAllBytesAsync(path, Array.Empty<byte>());
                    return;
                }

                var encryptedData = _encryptionService.Encrypt(content);
                await File.WriteAllBytesAsync(path, encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write encrypted file: {path}", ex);
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
                throw new InvalidOperationException($"Failed to delete encrypted file: {path}", ex);
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
                throw new InvalidOperationException($"Failed to copy encrypted file from {sourcePath} to {destinationPath}", ex);
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
                throw new InvalidOperationException($"Failed to move encrypted file from {sourcePath} to {destinationPath}", ex);
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
                throw new InvalidOperationException($"Failed to get encrypted files from directory: {directoryPath}", ex);
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
                throw new InvalidOperationException($"Failed to get encrypted file size: {path}", ex);
            }
        }

        /// <summary>
        /// Validates if encrypted file can be decrypted (integrity check)
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>True if file can be decrypted</returns>
        public async Task<bool> ValidateIntegrityAsync(string path)
        {
            ThrowIfDisposed();

            try
            {
                if (!File.Exists(path))
                    return false;

                var encryptedData = await File.ReadAllBytesAsync(path);
                if (encryptedData.Length == 0)
                    return true; // Empty file is valid

                return _encryptionService.ValidateIntegrity(encryptedData);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets encryption metadata for the file handler
        /// </summary>
        /// <returns>Encryption metadata</returns>
        public EncryptionMetadata GetEncryptionMetadata()
        {
            ThrowIfDisposed();
            return _encryptionService.GetMetadata();
        }

        /// <summary>
        /// Throws ObjectDisposedException if handler is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _encryptionService?.Dispose();
                _disposed = true;
            }
        }
    }
}