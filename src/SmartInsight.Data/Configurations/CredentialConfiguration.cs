using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Credential entity
/// </summary>
public class CredentialConfiguration : BaseMultiTenantEntityConfiguration<Credential>
{
    public override void Configure(EntityTypeBuilder<Credential> builder)
    {
        // Apply base configuration first
        base.Configure(builder);
        
        builder.ToTable("Credentials");
        
        // Add a unique constraint on tenant + key
        builder.HasIndex(c => new { c.TenantId, c.Key }).IsUnique();
        
        // Required fields
        builder.Property(c => c.Key).IsRequired().HasMaxLength(255);
        builder.Property(c => c.EncryptedValue).IsRequired();
        builder.Property(c => c.IV).IsRequired();
        
        // String fields with length constraints
        builder.Property(c => c.Source).HasMaxLength(100);
        builder.Property(c => c.Group).HasMaxLength(100);
        
        // Default values
        builder.Property(c => c.AccessCount).HasDefaultValue(0);
        builder.Property(c => c.IsEnabled).HasDefaultValue(true);
        
        // JSON columns
        builder.Property(c => c.Metadata).HasColumnType("jsonb");
        builder.Property(c => c.RotationHistory).HasColumnType("jsonb");
        
        // Indexes for common queries
        builder.HasIndex(c => c.Source);
        builder.HasIndex(c => c.Group);
        builder.HasIndex(c => c.IsEnabled);
        builder.HasIndex(c => c.ExpiresAt);
    }
} 