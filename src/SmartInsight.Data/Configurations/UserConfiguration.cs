using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the User entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        // Primary key
        builder.HasKey(u => u.Id);
        
        // Unique constraints
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        
        // Required fields
        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        
        // Optional fields with length constraints
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.ExternalId).HasMaxLength(256);
        builder.Property(u => u.ExternalProvider).HasMaxLength(50);
        
        // JSON columns
        builder.Property(u => u.Settings).HasColumnType("jsonb");
        
        // Default values
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.EmailVerified).HasDefaultValue(false);
        
        // Enums
        builder.Property(u => u.Role).HasConversion<string>();
        
        // Relationships
        builder.HasOne(u => u.PrimaryTenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.PrimaryTenantId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Ignore computed fields
        builder.Ignore(u => u.FullName);
    }
} 