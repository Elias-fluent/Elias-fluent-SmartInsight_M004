using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Tenant entity
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        
        // Primary key
        builder.HasKey(t => t.Id);
        
        // Unique constraints
        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.RlsIdentifier).IsUnique();
        
        // Required fields
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.ContactEmail).IsRequired().HasMaxLength(256);
        builder.Property(t => t.RlsIdentifier).IsRequired().HasMaxLength(100);
        
        // Optional fields with length constraints
        builder.Property(t => t.Description).HasMaxLength(1000);
        
        // JSON columns
        builder.Property(t => t.Settings).HasColumnType("jsonb");
        
        // Default values
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        
        // Self-referencing relationship for parent-child tenants
        builder.HasOne(t => t.ParentTenant)
            .WithMany()
            .HasForeignKey(t => t.ParentTenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
} 