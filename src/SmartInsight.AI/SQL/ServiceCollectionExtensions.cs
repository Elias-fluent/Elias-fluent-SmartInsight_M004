using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using SmartInsight.AI.SQL.Validators;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Extension methods for setting up SQL services in an IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds SQL parameter validation services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlParameterValidation(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register core parameter validation services
            services.AddSingleton<IParameterValidator, ParameterValidator>();
            
            // Register specialized validators
            services.AddSingleton<DatabaseObjectValidator>();
            services.AddSingleton<SqlOperationValidator>();
            services.AddSingleton<ValueTypeValidator>();
            
            // Register ValidationRuleSet factory
            services.AddSingleton<ValidationRuleSet>(serviceProvider =>
            {
                var validator = serviceProvider.GetRequiredService<IParameterValidator>();
                var logger = serviceProvider.GetRequiredService<ILogger<ValidationRuleSet>>();
                return new ValidationRuleSet(validator, logger, "DefaultRuleSet", "Default validation rule set for SQL parameters");
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds SQL injection prevention services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlInjectionPrevention(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register SQL sanitization and validation services
            services.AddSingleton<ISqlSanitizer, SqlSanitizer>();
            services.AddSingleton<ISqlValidator, SqlValidator>();
            
            return services;
        }
        
        /// <summary>
        /// Adds SQL validation rules engine to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlValidationRulesEngine(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register SQL validation rules engine
            services.AddSingleton<ISqlValidationRulesEngine, SqlValidationRulesEngine>();
            
            return services;
        }
        
        /// <summary>
        /// Adds tenant scoping services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <param name="tenantColumnMappings">Dictionary of table names to tenant column names</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddTenantScoping(this IServiceCollection services, Dictionary<string, string>? tenantColumnMappings = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register tenant scoping service
            services.AddSingleton<ITenantScopingService>(sp => 
            {
                var logger = sp.GetRequiredService<ILogger<TenantScopingService>>();
                return new TenantScopingService(logger);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds all SQL services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <param name="tenantColumnMappings">Dictionary of table names to tenant column names</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddAllSqlServices(this IServiceCollection services, Dictionary<string, string>? tenantColumnMappings = null)
        {
            return services
                .AddSqlParameterValidation()
                .AddSqlInjectionPrevention()
                .AddSqlValidationRulesEngine()
                .AddTenantScoping(tenantColumnMappings);
        }
    }
} 