using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Models
{
    /// <summary>
    /// Model representing user data
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// User identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Username for login
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// User's display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// User's first name
        /// </summary>
        public string? FirstName { get; set; }
        
        /// <summary>
        /// User's last name
        /// </summary>
        public string? LastName { get; set; }
        
        /// <summary>
        /// The tenant ID the user belongs to
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the user's email is confirmed
        /// </summary>
        public bool EmailConfirmed { get; set; }
        
        /// <summary>
        /// Whether the user is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Date and time when the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date and time when the user was last modified
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }
        
        /// <summary>
        /// Date and time when the user last logged in
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
        
        /// <summary>
        /// User preferences as a dictionary of key-value pairs
        /// </summary>
        public Dictionary<string, string>? Preferences { get; set; }
        
        /// <summary>
        /// List of roles assigned to the user
        /// </summary>
        public List<string>? Roles { get; set; }
        
        /// <summary>
        /// Whether the user is locked out
        /// </summary>
        public bool IsLockedOut { get; set; }
        
        /// <summary>
        /// Date and time when the lockout ends
        /// </summary>
        public DateTime? LockoutEnd { get; set; }
        
        /// <summary>
        /// User's profile image URL
        /// </summary>
        public string? ProfileImageUrl { get; set; }
    }
} 