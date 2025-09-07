// Security/EncryptionConfig.cs
using SimpleDataEngine.Storage.Hierarchical;
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
}
