namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Encryption configuration settings
    /// </summary>
    public class EncryptionConfig
    {
        /// <summary>
        /// Whether encryption is enabled
        /// </summary>
        public bool EnableEncryption { get; set; } = false;

        /// <summary>
        /// Custom password for encryption (null = use advanced cryptographic seed)
        /// </summary>
        public string CustomPassword { get; set; } = null;

        /// <summary>
        /// Whether to compress data before encryption
        /// </summary>
        public bool CompressBeforeEncrypt { get; set; } = true;

        /// <summary>
        /// File extension for encrypted files
        /// </summary>
        public string FileExtension { get; set; } = ".sde"; // SimpleDataEngine Encrypted

        /// <summary>
        /// Encryption algorithm to use
        /// </summary>
        public EncryptionType EncryptionType { get; set; } = EncryptionType.AES256;

        /// <summary>
        /// Key derivation iterations for enhanced security
        /// </summary>
        public int KeyDerivationIterations { get; set; } = 10000;

        /// <summary>
        /// Whether to include integrity check
        /// </summary>
        public bool IncludeIntegrityCheck { get; set; } = true;
    }

    /// <summary>
    /// Supported encryption types
    /// </summary>
    public enum EncryptionType
    {
        None,
        AES128,
        AES256,
        ChaCha20
    }
}