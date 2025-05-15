using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartInsight.Core.Entities;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartInsight.Core.Security;

/// <summary>
/// Custom claims principal factory that adds tenant-related claims to the user's identity
/// </summary>
public class TenantClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    /// <summary>
    /// Initializes a new instance of the TenantClaimsPrincipalFactory
    /// </summary>
    /// <param name="userManager">ASP.NET Identity user manager</param>
    /// <param name="roleManager">ASP.NET Identity role manager</param>
    /// <param name="options">Identity options</param>
    public TenantClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    /// <summary>
    /// Creates claims for a user
    /// </summary>
    /// <param name="user">The user to create claims for</param>
    /// <returns>A ClaimsPrincipal representing the user</returns>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add tenant ID claim
        identity.AddClaim(new Claim(SmartInsightClaimTypes.TenantId, user.PrimaryTenantId.ToString()));

        // Add full name claim if available
        if (!string.IsNullOrEmpty(user.FullName))
        {
            identity.AddClaim(new Claim(SmartInsightClaimTypes.FullName, user.FullName));
        }

        // Add first and last name claims if available
        if (!string.IsNullOrEmpty(user.FirstName))
        {
            identity.AddClaim(new Claim(SmartInsightClaimTypes.FirstName, user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            identity.AddClaim(new Claim(SmartInsightClaimTypes.LastName, user.LastName));
        }

        // Add IsActive claim
        identity.AddClaim(new Claim(SmartInsightClaimTypes.IsActive, user.IsActive.ToString().ToLower()));

        return identity;
    }
} 