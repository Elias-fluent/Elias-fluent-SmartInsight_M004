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
            // Register the parameter validator and extractor
            services.AddSingleton<IParameterValidator, ParameterValidator>();
            services.AddSingleton<IParameterExtractor, ParameterExtractor>();
            
            // Register specialized validators
            services.AddSingleton<DatabaseObjectValidator>();
            services.AddSingleton<SqlOperationValidator>();
            services.AddSingleton<ValueTypeValidator>();
            
            // Register ValidationRuleSet factory
            services.AddSingleton<ValidationRuleSet>(provider => {
                var validator = provider.GetRequiredService<IParameterValidator>();
                var logger = provider.GetRequiredService<ILogger<ValidationRuleSet>>();
                var ruleSet = new ValidationRuleSet(validator, logger, "DefaultRuleSet", "Default validation rule set for SQL parameters");
                
                // Add various rules
                ruleSet.AddRule("Required.Missing");
                ruleSet.AddRule("Type.Invalid");
                ruleSet.AddRule("Email.Invalid");
                ruleSet.AddRule("Url.Invalid");
                ruleSet.AddRule("DatabaseObject.Invalid");
                
                return ruleSet;
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds tenant scoping enforcement services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddTenantScopingEnforcement(this IServiceCollection services)
        {
            // Register tenant scoping service
            services.AddSingleton<ITenantScopingService, TenantScopingService>();
            
            return services;
        }

        /// <summary>
        /// Adds common validation rule sets to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the rule sets to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddValidationRuleSets(this IServiceCollection services)
        {
            // Register security rule set
            services.AddSingleton(provider => {
                var validator = provider.GetRequiredService<IParameterValidator>();
                var logger = provider.GetRequiredService<ILogger<ValidationRuleSet>>();
                
                return new ValidationRuleSet(validator, logger, "Security", "Security validation rules")
                    .AddRule("Security.Injection")
                    .AddRule("Security.SensitiveData")
                    .AddRule("Security.UnfilteredQuery")
                    .AddRule("Security.ObjectNameInjection");
            });
            
            // Register performance rule set
            services.AddSingleton(provider => {
                var validator = provider.GetRequiredService<IParameterValidator>();
                var logger = provider.GetRequiredService<ILogger<ValidationRuleSet>>();
                
                return new ValidationRuleSet(validator, logger, "Performance", "Performance validation rules")
                    .AddRule("Performance.ExcessiveLimit")
                    .AddRule("Performance.NoFilter")
                    .AddRule("Performance.NonKeyDelete");
            });
            
            // Register data integrity rule set
            services.AddSingleton(provider => {
                var validator = provider.GetRequiredService<IParameterValidator>();
                var logger = provider.GetRequiredService<ILogger<ValidationRuleSet>>();
                
                return new ValidationRuleSet(validator, logger, "DataIntegrity", "Data integrity validation rules")
                    .AddRule("DataIntegrity.EmptyGuid")
                    .AddRule("DataIntegrity.InvalidEmail")
                    .AddRule("DataIntegrity.InvalidUrl")
                    .AddRule("DataIntegrity.InvalidPhoneNumber");
            });
            
            // Register business logic rule set
            services.AddSingleton(provider => {
                var validator = provider.GetRequiredService<IParameterValidator>();
                var logger = provider.GetRequiredService<ILogger<ValidationRuleSet>>();
                
                return new ValidationRuleSet(validator, logger, "BusinessLogic", "Business logic validation rules")
                    .AddRule("Business.NegativeCurrency")
                    .AddRule("Business.LargeCurrency")
                    .AddRule("Business.UnknownSchema");
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds SQL parameter extraction services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlParameterExtraction(this IServiceCollection services)
        {
            // Register the parameter extractor
            services.AddSingleton<IParameterExtractor, ParameterExtractor>();
            
            return services;
        }
        
        /// <summary>
        /// Adds all SQL template system services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlTemplateSystem(this IServiceCollection services)
        {
            // Add parameter validation
            services.AddSqlParameterValidation();
            
            // Add parameter extraction
            services.AddSqlParameterExtraction();
            
            // Add other SQL template system services here as they are implemented
            // For example: template repository, template selector, SQL generator, etc.
            
            return services;
        }
    }
} 