namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Metadata for encrypted files and operations
    /// </summary>
    public class EncryptionMetadata
    {
        /// <summary>
        /// Encryption algorithm used
        /// </summary>
        public EncryptionType Algorithm { get; set; }

        /// <summary>
        /// File format version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// When the file was encrypted
        /// </summary>
        public DateTime EncryptedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Salt used for key derivation
        /// </summary>
        public byte[] Salt { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Initialization vector for encryption
        /// </summary>
        public byte[] IV { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Hash of the original data for integrity verification
        /// </summary>
        public byte[] DataHash { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Whether data was compressed before encryption
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Original file size before compression/encryption
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// Compressed size (if compression was used)
        /// </summary>
        public long CompressedSize { get; set; }

        /// <summary>
        /// Encrypted size
        /// </summary>
        public long EncryptedSize { get; set; }

        /// <summary>
        /// Key derivation iterations count
        /// </summary>
        public int KeyDerivationIterations { get; set; } = 100000;

        /// <summary>
        /// Additional metadata properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the metadata integrity
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Version) &&
                   Salt.Length > 0 &&
                   IV.Length > 0 &&
                   DataHash.Length > 0 &&
                   OriginalSize > 0 &&
                   KeyDerivationIterations > 0;
        }

        /// <summary>
        /// Creates metadata for new encryption operation
        /// </summary>
        public static EncryptionMetadata CreateNew(EncryptionType algorithm, long originalSize, bool compressed = false)
        {
            var metadata = new EncryptionMetadata
            {
                Algorithm = algorithm,
                OriginalSize = originalSize,
                IsCompressed = compressed,
                EncryptedAt = DateTime.UtcNow
            };

            // Generate random salt and IV
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                metadata.Salt = new byte[32]; // 256-bit salt
                metadata.IV = new byte[16];   // 128-bit IV for AES

                rng.GetBytes(metadata.Salt);
                rng.GetBytes(metadata.IV);
            }

            return metadata;
        }

        /// <summary>
        /// Serializes metadata to byte array
        /// </summary>
        public byte[] ToBytes()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserializes metadata from byte array
        /// </summary>
        public static EncryptionMetadata FromBytes(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return System.Text.Json.JsonSerializer.Deserialize<EncryptionMetadata>(json) ?? new EncryptionMetadata();
        }
    }
}