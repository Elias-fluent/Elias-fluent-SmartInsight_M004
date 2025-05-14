using System.Security.Cryptography;
using System.Text;

namespace SmartInsight.Core.Security;

/// <summary>
/// Utility for generating and validating secure tokens
/// </summary>
public static class TokenGenerator
{
    private const int DefaultTokenLength = 32;
    
    /// <summary>
    /// Generates a secure random token
    /// </summary>
    /// <param name="length">Length of the token in bytes (default is 32)</param>
    /// <returns>Base64-encoded secure random token</returns>
    public static string GenerateToken(int length = DefaultTokenLength)
    {
        var tokenBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }
    
    /// <summary>
    /// Generates a time-limited token with expiration
    /// </summary>
    /// <param name="expirationMinutes">Number of minutes until token expires</param>
    /// <param name="secret">Secret key for validating token</param>
    /// <returns>Token string with expiration information</returns>
    public static string GenerateTimedToken(int expirationMinutes, string secret)
    {
        // Create expiration timestamp
        var expiration = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds();
        
        // Generate random part
        var randomPart = GenerateToken(16);
        
        // Combine into payload
        var payload = $"{randomPart}:{expiration}";
        
        // Sign the payload
        var signature = ComputeSignature(payload, secret);
        
        // Combine everything
        return $"{payload}:{signature}";
    }
    
    /// <summary>
    /// Validates a timed token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <param name="secret">Secret key used to generate token</param>
    /// <returns>True if token is valid and not expired, otherwise false</returns>
    public static bool ValidateTimedToken(string token, string secret)
    {
        if (string.IsNullOrEmpty(token))
            return false;
            
        // Split token parts
        var parts = token.Split(':');
        if (parts.Length != 3)
            return false;
            
        var randomPart = parts[0];
        var expirationStr = parts[1];
        var providedSignature = parts[2];
        
        // Verify expiration
        if (!long.TryParse(expirationStr, out var expiration))
            return false;
            
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now > expiration)
            return false;
            
        // Verify signature
        var payload = $"{randomPart}:{expirationStr}";
        var expectedSignature = ComputeSignature(payload, secret);
        
        return string.Equals(providedSignature, expectedSignature, StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Computes a SHA256 HMAC signature for a payload
    /// </summary>
    /// <param name="payload">Data to sign</param>
    /// <param name="secret">Secret key</param>
    /// <returns>Signature as a hexadecimal string</returns>
    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
    }
} 