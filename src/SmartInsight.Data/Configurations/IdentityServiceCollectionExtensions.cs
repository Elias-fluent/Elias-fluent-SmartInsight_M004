using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Security;
using SmartInsight.Data.Contexts;
using System;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Extension methods for setting up ASP.NET Identity services
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registers core Identity-related services without the HTTP/Authorization components
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIdentityCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Identity core services
        var identityBuilder = services.AddIdentityCore<ApplicationUser>(options =>
        {
            // Configure password requirements
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            
            // Configure user requirements
            options.User.RequireUniqueEmail = true;
            
            // Configure lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            
            // Optional: Read configuration values manually if needed
            var identitySection = configuration.GetSection("Identity");
            if (identitySection != null)
            {
                var passwordSection = identitySection.GetSection("Password");
                if (passwordSection != null)
                {
                    if (int.TryParse(passwordSection["RequiredLength"], out int requiredLength))
                    {
                        options.Password.RequiredLength = requiredLength;
                    }
                }
            }
        });
        
        // Configure additional services
        identityBuilder
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        
        // Add tenant accessor that doesn't depend on HTTP
        services.AddScoped<ITenantAccessor, DefaultTenantAccessor>();
        
        return services;
    }
    
    /// <summary>
    /// NOTE: This method will be moved to a web project that has ASP.NET Core references.
    /// It is kept here as documentation of the complete implementation.
    /// </summary>
    public static void AddWebIdentityServices()
    {
        /*
        // This method would include:
        // 1. Call to AddIdentityCore above
        // 2. Configuration of HttpContextAccessor and HttpContextTenantAccessor
        // 3. Authorization policies setup
        // 4. Claims Principal Factory registration
        
        Example:
        
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantAccessor, HttpContextTenantAccessor>();
        
        services.AddAuthentication(options => {...})
                .AddCookie(...)
                
        services.AddAuthorization(options => 
        {
            options.AddPolicy("TenantAdmin", policy => 
                policy.RequireRole("Admin")
                      .RequireClaim(SmartInsightClaimTypes.CanAccessAllTenants, "true"));
        });
        */
    }
} 