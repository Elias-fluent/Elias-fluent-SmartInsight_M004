using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the ApplicationUser entity
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>
    /// Configures the application user entity
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        
        // Configure primary key
        builder.HasKey(u => u.Id);
        
        // Configure properties
        builder.Property(u => u.UserName).HasMaxLength(256);
        builder.Property(u => u.NormalizedUserName).HasMaxLength(256);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256);
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.ExternalId).HasMaxLength(256);
        builder.Property(u => u.ExternalProvider).HasMaxLength(50);
        
        // Configure indexes
        builder.HasIndex(u => u.NormalizedUserName).IsUnique();
        builder.HasIndex(u => u.NormalizedEmail);
        builder.HasIndex(u => u.PrimaryTenantId);
        
        // Configure default values
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        
        // Configure JSON columns
        builder.Property(u => u.Settings).HasColumnType("jsonb");
        
        // Create a global query filter for soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);
        
        // Configure relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.PrimaryTenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
} 