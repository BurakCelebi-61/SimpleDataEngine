using SimpleDataEngine.Audit;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Advanced encryption service for SimpleDataEngine
    /// </summary>
    public class EncryptionService : IDisposable
    {
        private readonly byte[] _key;
        private readonly EncryptionConfig _config;
        private bool _disposed = false;

        /// <summary>
        /// Initializes encryption service with configuration
        /// </summary>
        /// <param name="config">Encryption configuration</param>
        public EncryptionService(EncryptionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _key = CryptoKeyGenerator.GenerateSecureKey(
                config.CustomPassword,
                GetKeySize(config.EncryptionType),
                config.KeyDerivationIterations);

            if (!CryptoKeyGenerator.ValidateKeyEntropy(_key))
            {
                throw new InvalidOperationException("Generated encryption key has insufficient entropy");
            }

            AuditLogger.Log("ENCRYPTION_SERVICE_INITIALIZED", new
            {
                EncryptionType = config.EncryptionType,
                CompressionEnabled = config.CompressBeforeEncrypt,
                IntegrityCheckEnabled = config.IncludeIntegrityCheck
            }, category: AuditCategory.Security);
        }

        /// <summary>
        /// Encrypts string data
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <returns>Encrypted data as byte array</returns>
        public byte[] Encrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();

            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                if (_config.CompressBeforeEncrypt)
                {
                    dataBytes = CompressData(dataBytes);
                }

                var encryptedData = PerformEncryption(dataBytes);

                AuditLogger.Log("DATA_ENCRYPTED", new
                {
                    OriginalSize = data.Length,
                    CompressedSize = dataBytes.Length,
                    EncryptedSize = encryptedData.Length,
                    CompressionRatio = _config.CompressBeforeEncrypt ? (double)dataBytes.Length / data.Length : 1.0
                }, category: AuditCategory.Security);

                return encryptedData;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("ENCRYPTION_FAILED", ex, category: AuditCategory.Security);
                throw new InvalidOperationException("Failed to encrypt data", ex);
            }
        }

        /// <summary>
        /// Decrypts data back to string
        /// </summary>
        /// <param name="encryptedData">Encrypted data</param>
        /// <returns>Decrypted string</returns>
        public string Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return string.Empty;

            try
            {
                byte[] decryptedBytes = PerformDecryption(encryptedData);

                if (_config.CompressBeforeEncrypt)
                {
                    decryptedBytes = DecompressData(decryptedBytes);
                }

                var result = Encoding.UTF8.GetString(decryptedBytes);

                AuditLogger.Log("DATA_DECRYPTED", new
                {
                    EncryptedSize = encryptedData.Length,
                    DecryptedSize = result.Length
                }, category: AuditCategory.Security);

                return result;
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("DECRYPTION_FAILED", ex, category: AuditCategory.Security);
                throw new InvalidOperationException("Failed to decrypt data", ex);
            }
        }

        /// <summary>
        /// Validates if data can be decrypted (integrity check)
        /// </summary>
        /// <param name="encryptedData">Encrypted data to validate</param>
        /// <returns>True if data can be decrypted</returns>
        public bool ValidateIntegrity(byte[] encryptedData)
        {
            try
            {
                Decrypt(encryptedData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets encryption metadata
        /// </summary>
        /// <returns>Encryption metadata</returns>
        public EncryptionMetadata GetMetadata()
        {
            return new EncryptionMetadata
            {
                EncryptionType = _config.EncryptionType,
                CompressionEnabled = _config.CompressBeforeEncrypt,
                IntegrityCheckEnabled = _config.IncludeIntegrityCheck,
                KeySize = GetKeySize(_config.EncryptionType),
                CreatedAt = DateTime.Now
            };
        }

        #region Private Methods

        private byte[] PerformEncryption(byte[] data)
        {
            return _config.EncryptionType switch
            {
                EncryptionType.AES128 or EncryptionType.AES256 => EncryptAES(data),
                EncryptionType.ChaCha20 => EncryptChaCha20(data),
                _ => throw new NotSupportedException($"Encryption type {_config.EncryptionType} is not supported")
            };
        }

        private byte[] PerformDecryption(byte[] encryptedData)
        {
            return _config.EncryptionType switch
            {
                EncryptionType.AES128 or EncryptionType.AES256 => DecryptAES(encryptedData),
                EncryptionType.ChaCha20 => DecryptChaCha20(encryptedData),
                _ => throw new NotSupportedException($"Encryption type {_config.EncryptionType} is not supported")
            };
        }

        private byte[] EncryptAES(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();

            // Write IV first
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            // Write encrypted data
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(data, 0, data.Length);
            }

            var result = msEncrypt.ToArray();

            // Add integrity check if enabled
            if (_config.IncludeIntegrityCheck)
            {
                result = AddIntegrityCheck(result);
            }

            return result;
        }

        private byte[] DecryptAES(byte[] encryptedData)
        {
            // Remove integrity check if present
            if (_config.IncludeIntegrityCheck)
            {
                encryptedData = ValidateAndRemoveIntegrityCheck(encryptedData);
            }

            using var aes = Aes.Create();
            aes.Key = _key;

            // Extract IV
            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msOutput = new MemoryStream();

            csDecrypt.CopyTo(msOutput);
            return msOutput.ToArray();
        }

        private byte[] EncryptChaCha20(byte[] data)
        {
            // ChaCha20 implementation would go here
            // For now, fall back to AES
            return EncryptAES(data);
        }

        private byte[] DecryptChaCha20(byte[] encryptedData)
        {
            // ChaCha20 implementation would go here
            // For now, fall back to AES
            return DecryptAES(encryptedData);
        }

        private byte[] CompressData(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private byte[] DecompressData(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            gzip.CopyTo(output);
            return output.ToArray();
        }

        private byte[] AddIntegrityCheck(byte[] data)
        {
            using var hmac = new HMACSHA256(_key);
            var hash = hmac.ComputeHash(data);

            var result = new byte[data.Length + hash.Length];
            Array.Copy(data, 0, result, 0, data.Length);
            Array.Copy(hash, 0, result, data.Length, hash.Length);

            return result;
        }

        private byte[] ValidateAndRemoveIntegrityCheck(byte[] dataWithHash)
        {
            const int hashSize = 32; // SHA256 hash size

            if (dataWithHash.Length < hashSize)
                throw new InvalidOperationException("Data too short to contain integrity check");

            var dataLength = dataWithHash.Length - hashSize;
            var data = new byte[dataLength];
            var providedHash = new byte[hashSize];

            Array.Copy(dataWithHash, 0, data, 0, dataLength);
            Array.Copy(dataWithHash, dataLength, providedHash, 0, hashSize);

            using var hmac = new HMACSHA256(_key);
            var computedHash = hmac.ComputeHash(data);

            if (!computedHash.SequenceEqual(providedHash))
                throw new InvalidOperationException("Data integrity check failed");

            return data;
        }

        private static int GetKeySize(EncryptionType encryptionType)
        {
            return encryptionType switch
            {
                EncryptionType.AES128 => 128,
                EncryptionType.AES256 => 256,
                EncryptionType.ChaCha20 => 256,
                _ => 256
            };
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clear sensitive data
                if (_key != null)
                {
                    Array.Clear(_key, 0, _key.Length);
                }

                AuditLogger.Log("ENCRYPTION_SERVICE_DISPOSED", category: AuditCategory.Security);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Encryption metadata information
    /// </summary>
    public class EncryptionMetadata
    {
        public EncryptionType EncryptionType { get; set; }
        public bool CompressionEnabled { get; set; }
        public bool IntegrityCheckEnabled { get; set; }
        public int KeySize { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}