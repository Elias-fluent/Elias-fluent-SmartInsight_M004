using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Security;
using SmartInsight.Data.Configurations;
using System;

namespace SmartInsight.API.Security;

/// <summary>
/// Extension methods for setting up ASP.NET Core Identity services for web applications
/// </summary>
public static class WebIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds web-specific identity services including authentication, authorization and HTTP context
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWebIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // First add the core identity services
        services.AddIdentityCore(configuration);
        
        // Add HttpContextAccessor
        services.AddHttpContextAccessor();
        
        // Register HTTP-based tenant accessor
        services.AddScoped<ITenantAccessor, HttpContextTenantAccessor>();
        
        // Register claims principal factory
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TenantClaimsPrincipalFactory>();
        
        // Configure cookie authentication
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = "SmartInsight.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromDays(1);
            options.SlidingExpiration = true;
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
        });
        
        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            // Policy for users who can see all tenants' data
            options.AddPolicy("CanAccessAllTenants", policy =>
                policy.RequireRole("Admin")
                      .RequireClaim(SmartInsightClaimTypes.CanAccessAllTenants, "true"));
                      
            // Policy for tenant administrators
            options.AddPolicy("TenantAdmin", policy =>
                policy.RequireRole("TenantAdmin"));
                
            // Policy for active users
            options.AddPolicy("ActiveUser", policy =>
                policy.RequireClaim(SmartInsightClaimTypes.IsActive, "true"));
        });
        
        return services;
    }
} 