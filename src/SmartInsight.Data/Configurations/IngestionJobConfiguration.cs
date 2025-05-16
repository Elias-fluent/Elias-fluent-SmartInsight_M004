using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the IngestionJobEntity
/// </summary>
public class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJobEntity>
{
    /// <summary>
    /// Configures the entity
    /// </summary>
    /// <param name="builder">Builder for configuring the entity</param>
    public void Configure(EntityTypeBuilder<IngestionJobEntity> builder)
    {
        builder.ToTable("IngestionJobs");
        
        // Primary Key
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).IsRequired().HasMaxLength(50);
        
        // Properties
        builder.Property(j => j.Name).IsRequired().HasMaxLength(200);
        builder.Property(j => j.Description).HasMaxLength(500);
        builder.Property(j => j.DataSourceId).IsRequired();
        builder.Property(j => j.Status).IsRequired();
        builder.Property(j => j.TenantId).IsRequired();
        builder.Property(j => j.CronExpression).HasMaxLength(100);
        builder.Property(j => j.CreatedAt).IsRequired();
        builder.Property(j => j.ModifiedAt).IsRequired();
        builder.Property(j => j.FailureCount).IsRequired();
        builder.Property(j => j.IsPaused).IsRequired();
        builder.Property(j => j.MaxRetryCount).IsRequired();
        
        // For long text fields
        builder.Property(j => j.ExtractionParametersJson).HasColumnType("text");
        builder.Property(j => j.LastExecutionResult).HasColumnType("text");
        builder.Property(j => j.NotificationConfigJson).HasColumnType("text");
        
        // Indexes
        builder.HasIndex(j => j.TenantId);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.DataSourceId);
        builder.HasIndex(j => new { j.TenantId, j.Name }).IsUnique();
    }
} 