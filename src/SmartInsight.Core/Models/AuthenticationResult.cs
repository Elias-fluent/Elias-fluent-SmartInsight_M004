using System;

namespace SmartInsight.Core.Models
{
    /// <summary>
    /// Result of an authentication attempt
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Whether the authentication was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// The user ID if authentication was successful
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// The username of the authenticated user
        /// </summary>
        public string? Username { get; set; }
        
        /// <summary>
        /// The JWT token for API access if authentication was successful
        /// </summary>
        public string? Token { get; set; }
        
        /// <summary>
        /// The refresh token for obtaining new JWT tokens
        /// </summary>
        public string? RefreshToken { get; set; }
        
        /// <summary>
        /// When the token expires
        /// </summary>
        public DateTime TokenExpiration { get; set; }
        
        /// <summary>
        /// The tenant ID of the authenticated user
        /// </summary>
        public string? TenantId { get; set; }
        
        /// <summary>
        /// Error message if authentication failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Number of login attempts remaining before lockout
        /// </summary>
        public int? AttemptsRemaining { get; set; }
        
        /// <summary>
        /// Timestamp when the user will be allowed to retry login after lockout
        /// </summary>
        public DateTime? LockoutEnd { get; set; }
    }
} 