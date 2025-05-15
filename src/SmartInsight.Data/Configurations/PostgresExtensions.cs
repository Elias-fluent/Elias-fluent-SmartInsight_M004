using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// PostgreSQL-specific extensions and helper methods for Entity Framework Core
/// </summary>
public static class PostgresExtensions
{
    /// <summary>
    /// Applies common PostgreSQL configuration to the model builder
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure</param>
    public static void ConfigurePostgresFeatures(this ModelBuilder modelBuilder)
    {
        // Enable required extensions
        modelBuilder.HasPostgresExtension("uuid-ossp");    // For UUID generation
        modelBuilder.HasPostgresExtension("pg_trgm");      // For trigram-based text search
        modelBuilder.HasPostgresExtension("btree_gin");    // For GIN indexes on B-tree-indexable operations
        modelBuilder.HasPostgresExtension("pgcrypto");     // For cryptographic functions
    }
    
    /// <summary>
    /// Creates a case-insensitive index on a text column
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    /// <param name="propertyExpression">Expression to select the property</param>
    /// <returns>Index builder for further configuration</returns>
    public static IndexBuilder HasCaseInsensitiveIndex<T>(
        this EntityTypeBuilder<T> builder, 
        Expression<Func<T, string?>> propertyExpression) 
        where T : class
    {
        string propertyName = GetPropertyName(propertyExpression);
        return builder
            .HasIndex($"\"{propertyName}\" lower")
            .HasMethod("btree");
    }
    
    /// <summary>
    /// Configures a column for full-text search using GIN index
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    /// <param name="propertyExpression">Expression to select the property</param>
    /// <returns>Index builder for further configuration</returns>
    public static IndexBuilder HasFullTextSearchIndex<T>(
        this EntityTypeBuilder<T> builder, 
        Expression<Func<T, object?>> propertyExpression) 
        where T : class
    {
        return builder
            .HasIndex(propertyExpression)
            .HasMethod("GIN")
            .HasOperators("gin_trgm_ops");
    }
    
    /// <summary>
    /// Configures a JSONB column for search capabilities
    /// </summary>
    /// <param name="builder">The property builder</param>
    /// <returns>The property builder for chaining</returns>
    public static PropertyBuilder<string?> UseJsonbColumn<T>(
        this PropertyBuilder<string?> builder) 
        where T : class
    {
        return builder
            .HasColumnType("jsonb")
            .HasConversion(
                v => v, // No conversion needed when saving
                v => string.IsNullOrEmpty(v) ? "{}" : v); // Default empty JSON object when loading NULL
    }
    
    /// <summary>
    /// Helper method to extract property name from expression
    /// </summary>
    private static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is not MemberExpression member)
            throw new ArgumentException("Expression is not a valid property expression", nameof(propertyExpression));
            
        return member.Member.Name;
    }
    
    /// <summary>
    /// Normalizes a PostgreSQL table or column name by applying snake_case
    /// </summary>
    /// <param name="name">The name to normalize</param>
    /// <returns>Snake case normalized name</returns>
    public static string ToSnakeCase(this string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
            
        return Regex.Replace(
            Regex.Replace(
                Regex.Replace(name, @"([A-Z]+)([A-Z][a-z])", "$1_$2"), 
                @"([a-z\d])([A-Z])", "$1_$2"),
            @"[-\s]", "_").ToLower();
    }
} 