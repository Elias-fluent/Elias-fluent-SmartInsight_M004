using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartInsight.Core.DTOs;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Security;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SmartInsight.API.Security;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    /// <summary>
    /// Initializes a new instance of JwtTokenService
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger</param>
    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <param name="roles">User's roles</param>
    /// <param name="tenantId">User's tenant ID</param>
    /// <returns>Token details including access and refresh tokens</returns>
    public async Task<TokenDto> GenerateTokenAsync(ApplicationUser user, IList<string> roles, Guid? tenantId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(GetSecretKey());
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(SmartInsightClaimTypes.Username, user.UserName ?? string.Empty),
            new Claim(SmartInsightClaimTypes.IsActive, user.IsActive.ToString().ToLower())
        };
        
        // Add display name if available
        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            claims.Add(new Claim(SmartInsightClaimTypes.DisplayName, user.DisplayName));
        }
        
        // Add tenant ID if available
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            claims.Add(new Claim(SmartInsightClaimTypes.TenantId, tenantId.Value.ToString()));
        }
        
        // Add claims for roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            
            // Special handling for admin roles
            if (role == "Admin")
            {
                claims.Add(new Claim(SmartInsightClaimTypes.CanAccessAllTenants, "true"));
            }
        }
        
        // Set token expiration
        var tokenExpiration = GetTokenExpiration();
        var refreshTokenExpiration = GetRefreshTokenExpiration();
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(tokenExpiration),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();
        
        // Log token generation
        _logger.LogInformation("Generated JWT token for user {UserId} with {ClaimCount} claims", 
            user.Id, claims.Count);
            
        // This will be executed asynchronously in a real implementation
        // For example, storing the refresh token in a database
        await Task.CompletedTask;
        
        return new TokenDto
        {
            AccessToken = tokenHandler.WriteToken(token),
            ExpiresIn = (long)tokenExpiration.TotalSeconds,
            TokenType = "Bearer",
            RefreshToken = refreshToken,
            RefreshTokenExpiresIn = (long)refreshTokenExpiration.TotalSeconds
        };
    }
    
    /// <summary>
    /// Validates the provided JWT token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(GetSecretKey());
        
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }
    
    /// <summary>
    /// Gets claims from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims from the token</returns>
    public IEnumerable<Claim> GetClaimsFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract claims from token");
            return new List<Claim>();
        }
    }
    
    /// <summary>
    /// Generates a new refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    
    /// <summary>
    /// Gets the JWT secret key from configuration
    /// </summary>
    /// <returns>Secret key</returns>
    private string GetSecretKey()
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning("JWT secret key not found in configuration. Using default key (NOT SECURE FOR PRODUCTION)");
            return "SmartInsight_Default_Secret_Key_DO_NOT_USE_IN_PRODUCTION_1234567890";
        }
        
        return secretKey;
    }
    
    /// <summary>
    /// Gets the token expiration timespan from configuration
    /// </summary>
    /// <returns>Token expiration timespan</returns>
    private TimeSpan GetTokenExpiration()
    {
        var minutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes");
        return minutes > 0 
            ? TimeSpan.FromMinutes(minutes) 
            : TimeSpan.FromMinutes(60); // Default 1 hour
    }
    
    /// <summary>
    /// Gets the refresh token expiration timespan from configuration
    /// </summary>
    /// <returns>Refresh token expiration timespan</returns>
    private TimeSpan GetRefreshTokenExpiration()
    {
        var days = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");
        return days > 0 
            ? TimeSpan.FromDays(days) 
            : TimeSpan.FromDays(7); // Default 7 days
    }
} 