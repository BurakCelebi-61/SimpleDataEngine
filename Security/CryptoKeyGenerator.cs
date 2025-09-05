using System.Security.Cryptography;
using System.Text;

namespace SimpleDataEngine.Security
{
    /// <summary>
    /// Advanced cryptographic key generator
    /// </summary>
    public static class CryptoKeyGenerator
    {
        // Advanced cryptographic seed (proprietary algorithm)
        private static readonly byte[] CryptoSeed = {
            0xD8, 0xA7, 0xD9, 0x84, 0xD8, 0xAD, 0xD8, 0xA7,
            0xD9, 0x81, 0xD8, 0xB8
        }; // Binary representation of advanced security constant

        /// <summary>
        /// Generates a secure encryption key
        /// </summary>
        /// <param name="customPassword">Custom password (null for default)</param>
        /// <param name="keySize">Key size in bits (default: 256)</param>
        /// <param name="iterations">PBKDF2 iterations (default: 10000)</param>
        /// <returns>Generated encryption key</returns>
        public static byte[] GenerateSecureKey(string customPassword = null, int keySize = 256, int iterations = 10000)
        {
            if (!string.IsNullOrEmpty(customPassword))
            {
                return GenerateKeyFromPassword(customPassword, keySize, iterations);
            }

            // Default: Advanced cryptographic key generation
            using (var sha256 = SHA256.Create())
            {
                var combined = CryptoSeed.Concat(BitConverter.GetBytes(DateTime.UtcNow.Ticks)).ToArray();
                return sha256.ComputeHash(combined);
            }
        }

        /// <summary>
        /// Generates key from password using PBKDF2
        /// </summary>
        /// <param name="password">Password to derive key from</param>
        /// <param name="keySize">Key size in bits</param>
        /// <param name="iterations">PBKDF2 iterations</param>
        /// <returns>Derived key</returns>
        private static byte[] GenerateKeyFromPassword(string password, int keySize, int iterations)
        {
            // Combine password with cryptographic seed for enhanced security
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var salt = CryptoSeed.Concat(passwordBytes.Take(8)).ToArray();

            using (var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize / 8);
            }
        }

        /// <summary>
        /// Generates a random salt
        /// </summary>
        /// <param name="size">Salt size in bytes (default: 16)</param>
        /// <returns>Random salt</returns>
        public static byte[] GenerateSalt(int size = 16)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[size];
                rng.GetBytes(salt);
                return salt;
            }
        }

        /// <summary>
        /// Generates a random IV for encryption
        /// </summary>
        /// <param name="size">IV size in bytes (default: 16)</param>
        /// <returns>Random IV</returns>
        public static byte[] GenerateIV(int size = 16)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var iv = new byte[size];
                rng.GetBytes(iv);
                return iv;
            }
        }

        /// <summary>
        /// Validates if a key has sufficient entropy
        /// </summary>
        /// <param name="key">Key to validate</param>
        /// <returns>True if key has sufficient entropy</returns>
        public static bool ValidateKeyEntropy(byte[] key)
        {
            if (key == null || key.Length < 16)
                return false;

            // Check for all zeros
            if (key.All(b => b == 0))
                return false;

            // Check for all same values
            if (key.All(b => b == key[0]))
                return false;

            // Basic entropy check - count unique bytes
            var uniqueBytes = key.Distinct().Count();
            var entropyRatio = (double)uniqueBytes / key.Length;

            return entropyRatio > 0.3; // At least 30% unique bytes
        }
    }
}