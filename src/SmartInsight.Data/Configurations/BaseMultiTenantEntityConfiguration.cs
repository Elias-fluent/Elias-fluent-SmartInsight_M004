using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Base configuration class for all multi-tenant entities
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseMultiTenantEntity</typeparam>
public abstract class BaseMultiTenantEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> 
    where TEntity : BaseMultiTenantEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);
        
        // Created/updated timestamps
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(e => e.UpdatedAt).IsRequired(false);
        builder.Property(e => e.UpdatedBy).IsRequired(false).HasMaxLength(256);
        
        // Soft delete
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        
        // Multi-tenant configuration
        builder.Property(e => e.TenantId).IsRequired();
        
        // Create index on tenant ID for faster filtering
        builder.HasIndex(e => e.TenantId);
        
        // Configure relationship to tenant
        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
} 