using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the ApplicationRole entity
/// </summary>
public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    /// <summary>
    /// Configures the application role entity
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");
        
        // Configure primary key
        builder.HasKey(r => r.Id);
        
        // Configure properties
        builder.Property(r => r.Name).HasMaxLength(256);
        builder.Property(r => r.NormalizedName).HasMaxLength(256);
        builder.Property(r => r.Description).HasMaxLength(500);
        
        // Configure indexes
        builder.HasIndex(r => r.NormalizedName).IsUnique();
        builder.HasIndex(r => r.TenantId);
        
        // Configure default values
        builder.Property(r => r.IsSystemRole).HasDefaultValue(false);
        
        // Configure relationships (if tenant ID is not null)
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Create a composite index on tenant ID and normalized name for tenant-specific role uniqueness
        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .HasFilter("\"TenantId\" IS NOT NULL")
            .IsUnique();
    }
} 