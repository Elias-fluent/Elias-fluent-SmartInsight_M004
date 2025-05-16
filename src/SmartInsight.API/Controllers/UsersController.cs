using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.DTOs;
using SmartInsight.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInsight.API.Controllers;

/// <summary>
/// Controller for user management operations
/// </summary>
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class UsersController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the UsersController
    /// </summary>
    public UsersController(
        UserManager<ApplicationUser> userManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users in the tenant
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAllUsers()
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue && !CanAccessAllTenants())
        {
            _logger.LogWarning("User {UserId} attempted to access users without a valid tenant ID", GetCurrentUserId());
            return Forbid();
        }

        // Get all users
        var users = _userManager.Users.Where(u => 
            tenantId == null || CanAccessAllTenants() || u.PrimaryTenantId == tenantId).ToList();
            
        var userDtos = new List<UserDto>();
        
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName ?? string.Empty,
                IsActive = user.IsActive,
                TenantId = user.PrimaryTenantId,
                Roles = roles.ToList()
            });
        }
        
        return Ok(userDtos);
    }

    /// <summary>
    /// Gets a specific user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(string id)
    {
        var tenantId = GetCurrentTenantId();
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }
        
        // Validate tenant access
        if (!CanAccessAllTenants() && user.PrimaryTenantId != tenantId)
        {
            _logger.LogWarning("User {UserId} attempted to access user from another tenant", GetCurrentUserId());
            return Forbid();
        }
        
        var roles = await _userManager.GetRolesAsync(user);
        
        var userDto = new UserDto
        {
            Id = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            IsActive = user.IsActive,
            TenantId = user.PrimaryTenantId,
            Roles = roles.ToList()
        };
        
        return Ok(userDto);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="model">User details</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var tenantId = GetCurrentTenantId();
        
        // Validate tenant access
        if (!CanAccessAllTenants() && model.TenantId != tenantId)
        {
            _logger.LogWarning("User {UserId} attempted to create user for another tenant", GetCurrentUserId());
            return Forbid();
        }
        
        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            DisplayName = model.DisplayName,
            IsActive = model.IsActive,
            PrimaryTenantId = model.TenantId,
            EmailConfirmed = true // For simplicity in this example
        };
        
        var result = await _userManager.CreateAsync(user, model.Password);
        
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return BadRequest(ModelState);
        }
        
        // Assign roles
        if (model.Roles != null && model.Roles.Any())
        {
            result = await _userManager.AddToRolesAsync(user, model.Roles);
            
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                // Cleanup: delete user if role assignment fails
                await _userManager.DeleteAsync(user);
                return BadRequest(ModelState);
            }
        }
        
        // Return the created user
        var roles = await _userManager.GetRolesAsync(user);
        
        var userDto = new UserDto
        {
            Id = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            IsActive = user.IsActive,
            TenantId = user.PrimaryTenantId,
            Roles = roles.ToList()
        };
        
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="model">Updated user details</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var tenantId = GetCurrentTenantId();
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }
        
        // Validate tenant access
        if (!CanAccessAllTenants() && user.PrimaryTenantId != tenantId)
        {
            _logger.LogWarning("User {UserId} attempted to update user from another tenant", GetCurrentUserId());
            return Forbid();
        }
        
        // Update user properties
        user.DisplayName = model.DisplayName;
        user.IsActive = model.IsActive;
        user.Email = model.Email;
        
        // Update username if provided
        if (!string.IsNullOrEmpty(model.UserName) && user.UserName != model.UserName)
        {
            user.UserName = model.UserName;
        }
        
        var result = await _userManager.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return BadRequest(ModelState);
        }
        
        // Update password if provided
        if (!string.IsNullOrEmpty(model.Password))
        {
            // Remove existing password and add new one
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            
            if (removePasswordResult.Succeeded)
            {
                var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
                
                if (!addPasswordResult.Succeeded)
                {
                    foreach (var error in addPasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    
                    return BadRequest(ModelState);
                }
            }
        }
        
        // Update roles if provided
        if (model.Roles != null && model.Roles.Any())
        {
            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Remove roles
            if (currentRoles.Any())
            {
                result = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    
                    return BadRequest(ModelState);
                }
            }
            
            // Add new roles
            result = await _userManager.AddToRolesAsync(user, model.Roles);
            
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                return BadRequest(ModelState);
            }
        }
        
        return NoContent();
    }

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var tenantId = GetCurrentTenantId();
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }
        
        // Validate tenant access
        if (!CanAccessAllTenants() && user.PrimaryTenantId != tenantId)
        {
            _logger.LogWarning("User {UserId} attempted to delete user from another tenant", GetCurrentUserId());
            return Forbid();
        }
        
        var result = await _userManager.DeleteAsync(user);
        
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return BadRequest(ModelState);
        }
        
        return NoContent();
    }
} 