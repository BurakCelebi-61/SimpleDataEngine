// Security/EncryptionConfig.cs
using System.IO.Compression;
using System.Security.Cryptography;


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
            using var aes = Aes.Create();
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

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(data, 0, data.Length);
            }

            return msEncrypt.ToArray();
        }

        private byte[] Decrypt(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = _config.CipherMode;
            aes.Padding = _config.PaddingMode;

            // Extract IV from the beginning of the data
            var ivSize = aes.BlockSize / 8;
            aes.IV = new byte[ivSize];
            Array.Copy(encryptedData, 0, aes.IV, 0, ivSize);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData, ivSize, encryptedData.Length - ivSize);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msResult = new MemoryStream();
            csDecrypt.CopyTo(msResult);
            return msResult.ToArray();
        }

        private byte[] Compress(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress);
            gzipStream.Write(data, 0, data.Length);
            gzipStream.Close();
            return compressedStream.ToArray();
        }

        private byte[] Decompress(byte[] compressedData)
        {
            using var compressedStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            gzipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keySize)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keySize);
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
