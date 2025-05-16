using System.Collections.Generic;

namespace SmartInsight.Core.Models
{
    /// <summary>
    /// Result of a user registration attempt
    /// </summary>
    public class RegistrationResult
    {
        /// <summary>
        /// Whether the registration was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// The user ID if registration was successful
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// Error message if registration failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// List of validation errors if registration failed due to validation
        /// </summary>
        public List<string>? ValidationErrors { get; set; }
        
        /// <summary>
        /// Whether email confirmation is required
        /// </summary>
        public bool RequiresEmailConfirmation { get; set; }
        
        /// <summary>
        /// The tenant ID of the registered user
        /// </summary>
        public string? TenantId { get; set; }
        
        /// <summary>
        /// Create a successful registration result
        /// </summary>
        /// <param name="userId">The ID of the newly registered user</param>
        /// <param name="tenantId">The tenant ID of the registered user</param>
        /// <param name="requiresEmailConfirmation">Whether email confirmation is required</param>
        /// <returns>A successful registration result</returns>
        public static RegistrationResult Success(string userId, string tenantId, bool requiresEmailConfirmation = false)
        {
            return new RegistrationResult
            {
                IsSuccess = true,
                UserId = userId,
                TenantId = tenantId,
                RequiresEmailConfirmation = requiresEmailConfirmation
            };
        }
        
        /// <summary>
        /// Create a failed registration result
        /// </summary>
        /// <param name="errorMessage">Error message explaining why registration failed</param>
        /// <returns>A failed registration result</returns>
        public static RegistrationResult Failure(string errorMessage)
        {
            return new RegistrationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Create a failed registration result due to validation errors
        /// </summary>
        /// <param name="validationErrors">List of validation errors</param>
        /// <returns>A failed registration result</returns>
        public static RegistrationResult ValidationFailure(List<string> validationErrors)
        {
            return new RegistrationResult
            {
                IsSuccess = false,
                ValidationErrors = validationErrors,
                ErrorMessage = "Registration failed due to validation errors."
            };
        }
    }
} 