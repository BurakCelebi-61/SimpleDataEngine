using SimpleDataEngine.Security;
using System.Security.Cryptography;
using System.Text;

namespace SimpleDataEngine.Storage.Hierarchical
{
    /// <summary>
    /// Encryption service for database operations
    /// </summary>
    public class EncryptionService : IDisposable
    {
        private readonly EncryptionConfig _config;
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private bool _disposed = false;

        public EncryptionService(EncryptionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Generate or derive key and IV based on configuration
            _key = DeriveKey(config.EncryptionKey ?? "DefaultKey", 32);
            _iv = DeriveKey(config.EncryptionKey ?? "DefaultKey", 16);
        }

        /// <summary>
        /// Encrypt string data
        /// </summary>
        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = await EncryptBytesAsync(plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypt string data
        /// </summary>
        public async Task<string> DecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = await DecryptBytesAsync(encryptedBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Encrypt byte array
        /// </summary>
        public async Task<byte[]> EncryptBytesAsync(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return plainBytes;

            return await Task.Run(() =>
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();

                return memoryStream.ToArray();
            });
        }

        /// <summary>
        /// Decrypt byte array
        /// </summary>
        public async Task<byte[]> DecryptBytesAsync(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0)
                return encryptedBytes;

            return await Task.Run(() =>
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var resultStream = new MemoryStream();

                cryptoStream.CopyTo(resultStream);
                return resultStream.ToArray();
            });
        }

        /// <summary>
        /// Derive key from password
        /// </summary>
        private byte[] DeriveKey(string password, int keyLength)
        {
            var salt = Encoding.UTF8.GetBytes("SimpleDataEngine"); // Fixed salt for simplicity

            using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return rfc2898.GetBytes(keyLength);
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Clear sensitive data
            Array.Clear(_key, 0, _key.Length);
            Array.Clear(_iv, 0, _iv.Length);

            _disposed = true;
        }
    }
}