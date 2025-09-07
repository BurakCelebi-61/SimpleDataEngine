// Security/EncryptionConfig.cs
using System.IO.Compression;
using System.Security.Cryptography;

namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Encryption configuration
    /// </summary>
    public class EncryptionConfig
    {
        public bool EnableEncryption { get; set; } = false;
        public EncryptionType EncryptionType { get; set; } = EncryptionType.AES256;
        public string EncryptionKey { get; set; } = string.Empty;
        public string? KeyDerivationSalt { get; set; }
        public int KeyDerivationIterations { get; set; } = 10000;
        public bool CompressBeforeEncrypt { get; set; } = true;
        public bool UseRandomIV { get; set; } = true;
        public int KeySize { get; set; } = 256;
        public CipherMode CipherMode { get; set; } = CipherMode.CBC;
        public PaddingMode PaddingMode { get; set; } = PaddingMode.PKCS7;

        /// <summary>
        /// Validate encryption configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (EnableEncryption)
            {
                if (string.IsNullOrEmpty(EncryptionKey))
                    result.AddError("EncryptionKey is required when encryption is enabled");

                if (EncryptionKey.Length < 16)
                    result.AddError("EncryptionKey must be at least 16 characters long");

                if (KeySize != 128 && KeySize != 192 && KeySize != 256)
                    result.AddError("KeySize must be 128, 192, or 256");

                if (KeyDerivationIterations < 1000)
                    result.AddError("KeyDerivationIterations should be at least 1000 for security");
            }

            return result;
        }

        /// <summary>
        /// Generate a secure random encryption key
        /// </summary>
        /// <param name="keySize">Key size in bits (128, 192, or 256)</param>
        /// <returns>Base64 encoded encryption key</returns>
        public static string GenerateSecureKey(int keySize = 256)
        {
            var keyBytes = new byte[keySize / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            return Convert.ToBase64String(keyBytes);
        }

        /// <summary>
        /// Generate a secure random salt
        /// </summary>
        /// <param name="saltSize">Salt size in bytes</param>
        /// <returns>Base64 encoded salt</returns>
        public static string GenerateSecureSalt(int saltSize = 32)
        {
            var saltBytes = new byte[saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }
    }

    /// <summary>
    /// Encryption types supported
    /// </summary>
    public enum EncryptionType
    {
        None = 0,
        AES128 = 1,
        AES192 = 2,
        AES256 = 3
    }
}

// Security/IFileHandler.cs
namespace SimpleDataEngine.Security
{
    /// <summary>
    /// File handler interface for storage operations
    /// </summary>
    public interface IFileHandler : IDisposable
    {
        /// <summary>
        /// Read file content
        /// </summary>
        Task<byte[]> ReadFileAsync(string filePath);

        /// <summary>
        /// Write file content
        /// </summary>
        Task WriteFileAsync(string filePath, byte[] content);

        /// <summary>
        /// Check if file exists
        /// </summary>
        bool FileExists(string filePath);

        /// <summary>
        /// Delete file
        /// </summary>
        Task DeleteFileAsync(string filePath);

        /// <summary>
        /// Get file size
        /// </summary>
        long GetFileSize(string filePath);

        /// <summary>
        /// Create directory if it doesn't exist
        /// </summary>
        void EnsureDirectoryExists(string directoryPath);
    }
}

// Security/StandardFileHandler.cs
namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Standard file handler without encryption
    /// </summary>
    public class StandardFileHandler : IFileHandler
    {
        private bool _disposed = false;

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task WriteFileAsync(string filePath, byte[] content)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, content);
        }

        public bool FileExists(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));

            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
        }

        public async Task DeleteFileAsync(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));

            if (string.IsNullOrEmpty(filePath))
                return;

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }

        public long GetFileSize(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return 0;

            return new FileInfo(filePath).Length;
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StandardFileHandler));

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}

// Security/EncryptedFileHandler.cs
using System.Security.Cryptography;
using System.IO.Compression;

namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Encrypted file handler with AES encryption
    /// </summary>
    public class EncryptedFileHandler : IFileHandler
    {
        private readonly EncryptionConfig _config;
        private readonly byte[] _key;
        private readonly byte[] _salt;
        private bool _disposed = false;

        public EncryptedFileHandler(EncryptionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var validation = _config.Validate();
            if (!validation.IsValid)
                throw new ArgumentException($"Invalid encryption config: {string.Join(", ", validation.Errors)}");

            // Derive key from password and salt
            _salt = !string.IsNullOrEmpty(_config.KeyDerivationSalt)
                ? Convert.FromBase64String(_config.KeyDerivationSalt)
                : System.Text.Encoding.UTF8.GetBytes("DefaultSalt_SimpleDataEngine_2025");

            _key = DeriveKey(_config.EncryptionKey, _salt, _config.KeyDerivationIterations, _config.KeySize / 8);
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = Decrypt(encryptedData);

            // Decompress if needed
            if (_config.CompressBeforeEncrypt)
            {
                return Decompress(decryptedData);
            }

            return decryptedData;
        }

        public async Task WriteFileAsync(string filePath, byte[] content)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var dataToEncrypt = content;

            // Compress if needed
            if (_config.CompressBeforeEncrypt)
            {
                dataToEncrypt = Compress(content);
            }

            var encryptedData = Encrypt(dataToEncrypt);
            await File.WriteAllBytesAsync(filePath, encryptedData);
        }

        public bool FileExists(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));

            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
        }

        public async Task DeleteFileAsync(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));

            if (string.IsNullOrEmpty(filePath))
                return;

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }

        public long GetFileSize(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return 0;

            return new FileInfo(filePath).Length;
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedFileHandler));

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private byte[] Encrypt(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.Mode = _config.CipherMode;
                aes.Padding = _config.PaddingMode;

                if (_config.UseRandomIV)
                {
                    aes.GenerateIV();
                }
                else
                {
                    aes.IV = new byte[aes.BlockSize / 8]; // Zero IV (less secure)
                }

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Write IV to the beginning of the stream
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }

                    return msEncrypt.ToArray();
                }
            }
        }

        private byte[] Decrypt(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.Mode = _config.CipherMode;
                aes.Padding = _config.PaddingMode;

                // Extract IV from the beginning of the data
                var ivSize = aes.BlockSize / 8;
                aes.IV = new byte[ivSize];
                Array.Copy(encryptedData, 0, aes.IV, 0, ivSize);

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(encryptedData, ivSize, encryptedData.Length - ivSize))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var msResult = new MemoryStream())
                {
                    csDecrypt.CopyTo(msResult);
                    return msResult.ToArray();
                }
            }
        }

        private byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                gzipStream.Close();
                return compressedStream.ToArray();
            }
        }

        private byte[] Decompress(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keySize)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clear sensitive data
                if (_key != null)
                {
                    Array.Clear(_key, 0, _key.Length);
                }

                _disposed = true;
            }
        }
    }
}

// Security/ValidationResult.cs (if not already defined)
namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Validation result helper class
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
                Errors.Add(error);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors ?? Enumerable.Empty<string>())
                AddError(error);
        }

        public override string ToString()
        {
            return IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
        }
    }
}