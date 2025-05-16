using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;

namespace SmartInsight.API.Security;

/// <summary>
/// Extension methods for setting up JWT authentication services
/// </summary>
public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// Adds JWT authentication services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            secretKey = "SmartInsight_Default_Secret_Key_DO_NOT_USE_IN_PRODUCTION_1234567890";
        }
        
        var key = Encoding.ASCII.GetBytes(secretKey);
        
        // Register JWT token service
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        
        // Configure JWT authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        
        return services;
    }
    
    /// <summary>
    /// Configures Swagger to support JWT authentication
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSwaggerWithJwtSupport(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo 
            {
                Title = "SmartInsight API",
                Version = "v1",
                Description = "REST API for SmartInsight",
                Contact = new OpenApiContact
                {
                    Name = "SmartInsight Team",
                    Email = "support@smartinsight.com"
                }
            });
            
            // Add JWT authentication support
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer [token]'"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        return services;
    }
} 