using System.Security.Cryptography;
using System.Text;

namespace SmartInsight.Core.Security;

/// <summary>
/// Utility for securely hashing and verifying passwords
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 10000; // Default iteration count
    
    /// <summary>
    /// Hashes a password using PBKDF2 with HMAC-SHA256
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <param name="iterations">Number of iterations (defaults to 10000)</param>
    /// <returns>Base64-encoded string containing salt and hash</returns>
    public static string HashPassword(string password, int iterations = Iterations)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);
        
        var hash = GetHash(password, salt, iterations);
        
        var hashBytes = new byte[SaltSize + KeySize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);
        
        return Convert.ToBase64String(hashBytes);
    }
    
    /// <summary>
    /// Verifies a password against a stored hash
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hashedPassword">Stored password hash</param>
    /// <param name="iterations">Number of iterations (defaults to 10000)</param>
    /// <returns>True if password matches the hash, otherwise false</returns>
    public static bool VerifyPassword(string password, string hashedPassword, int iterations = Iterations)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);
        
        // Extract salt
        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);
        
        // Extract hash
        var expectedHash = new byte[KeySize];
        Array.Copy(hashBytes, SaltSize, expectedHash, 0, KeySize);
        
        // Compute hash for provided password
        var actualHash = GetHash(password, salt, iterations);
        
        // Compare hashes
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
    
    private static byte[] GetHash(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }
} 