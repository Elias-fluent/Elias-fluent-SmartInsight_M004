using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartInsight.API.Security;
using SmartInsight.Core.DTOs;
using SmartInsight.Core.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInsight.API.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController
    /// </summary>
    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="model">Login credentials</param>
    /// <returns>JWT token information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Try to find user by email or username
        var user = await _userManager.FindByEmailAsync(model.UsernameOrEmail)
            ?? await _userManager.FindByNameAsync(model.UsernameOrEmail);
            
        if (user == null)
        {
            _logger.LogWarning("Login failed: User with identifier {Identifier} not found", model.UsernameOrEmail);
            return Unauthorized(new { error = "Invalid username/email or password" });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {UserId} is inactive", user.Id);
            return Unauthorized(new { error = "Account is inactive" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed: User {UserId} is locked out", user.Id);
                return Unauthorized(new { error = "Account is locked out. Try again later." });
            }

            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            return Unauthorized(new { error = "Invalid username/email or password" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = await _tokenService.GenerateTokenAsync(user, roles, user.PrimaryTenantId);

        _logger.LogInformation("User {UserId} successfully logged in", user.Id);
        return Ok(token);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="model">Refresh token request</param>
    /// <returns>New JWT token information</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // In a real implementation, you would validate the refresh token against a store
        // and retrieve the associated user. This is a simplified version.
        
        // For now, we'll extract the user ID from the token and generate a new token
        var claims = _tokenService.GetClaimsFromToken(model.AccessToken);
        var userId = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Refresh token failed: Invalid access token");
            return Unauthorized(new { error = "Invalid token" });
        }
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Refresh token failed: User {UserId} not found or inactive", userId);
            return Unauthorized(new { error = "Invalid token" });
        }
        
        // Retrieve the tenant ID from claims
        var tenantIdClaim = claims.FirstOrDefault(c => c.Type == Core.Security.SmartInsightClaimTypes.TenantId)?.Value;
        Guid? tenantId = null;
        
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var parsedTenantId))
        {
            tenantId = parsedTenantId;
        }
        
        var roles = await _userManager.GetRolesAsync(user);
        var token = await _tokenService.GenerateTokenAsync(user, roles, tenantId);
        
        _logger.LogInformation("Successfully refreshed token for user {UserId}", user.Id);
        return Ok(token);
    }
} 