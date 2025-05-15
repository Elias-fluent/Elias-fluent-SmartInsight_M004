using System.Security.Cryptography;
using System.Text;

namespace SmartInsight.Core.Security;

/// <summary>
/// Utility for encrypting and decrypting sensitive data
/// </summary>
public static class EncryptionUtility
{
    private const int KeySize = 32; // 256 bits
    private const int IvSize = 16; // 128 bits
    
    /// <summary>
    /// Encrypts sensitive data using AES
    /// </summary>
    /// <param name="plainText">Data to encrypt</param>
    /// <param name="key">Encryption key (will be hashed to ensure proper length)</param>
    /// <returns>Base64-encoded encrypted data</returns>
    public static string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;
            
        try
        {
            // Derive a key of the correct size
            var keyBytes = DeriveKeyBytes(key);
            
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = keyBytes;
            aes.GenerateIV();
            
            // Encrypt the data
            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            // Combine IV and ciphertext
            byte[] result = new byte[IvSize + cipherBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, IvSize);
            Array.Copy(cipherBytes, 0, result, IvSize, cipherBytes.Length);
            
            return Convert.ToBase64String(result);
        }
        catch (Exception)
        {
            throw new CryptographicException("Encryption failed");
        }
    }
    
    /// <summary>
    /// Decrypts encrypted data
    /// </summary>
    /// <param name="cipherText">Base64-encoded encrypted data</param>
    /// <param name="key">Encryption key (will be hashed to ensure proper length)</param>
    /// <returns>Decrypted data</returns>
    public static string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;
            
        try
        {
            // Derive a key of the correct size
            var keyBytes = DeriveKeyBytes(key);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            
            // Make sure we have at least the IV size
            if (cipherBytes.Length < IvSize)
                throw new CryptographicException("Invalid encrypted data");
                
            // Extract the IV
            byte[] iv = new byte[IvSize];
            Array.Copy(cipherBytes, 0, iv, 0, IvSize);
            
            // Extract the ciphertext
            byte[] encryptedData = new byte[cipherBytes.Length - IvSize];
            Array.Copy(cipherBytes, IvSize, encryptedData, 0, encryptedData.Length);
            
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = keyBytes;
            aes.IV = iv;
            
            // Decrypt the data
            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception)
        {
            throw new CryptographicException("Decryption failed");
        }
    }
    
    /// <summary>
    /// Derives a key of the correct size from a password
    /// </summary>
    /// <param name="password">Password to derive key from</param>
    /// <returns>Key bytes of the correct size</returns>
    private static byte[] DeriveKeyBytes(string password)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
} 