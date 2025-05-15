using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsight.Core.Entities;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the KnowledgeNode entity
/// </summary>
public class KnowledgeNodeConfiguration : BaseMultiTenantEntityConfiguration<KnowledgeNode>
{
    public override void Configure(EntityTypeBuilder<KnowledgeNode> builder)
    {
        // Apply base configuration first
        base.Configure(builder);
        
        builder.ToTable("KnowledgeNodes");
        
        // Required fields
        builder.Property(n => n.Title).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Type).IsRequired().HasMaxLength(50).HasDefaultValue("Concept");
        
        // Content with text search capability
        builder.Property(n => n.Content).HasColumnType("text");
        
        // Vector embeddings stored as JSON array
        builder.Property(n => n.VectorEmbedding).HasColumnType("jsonb");
        
        // JSON columns
        builder.Property(n => n.RelatedNodeIds).HasColumnType("jsonb");
        builder.Property(n => n.SemanticProperties).HasColumnType("jsonb");
        builder.Property(n => n.SourceDocumentIds).HasColumnType("jsonb");
        
        // String fields with length constraints
        builder.Property(n => n.Tags).HasMaxLength(500);
        builder.Property(n => n.VerifiedBy).HasMaxLength(256);
        
        // Default values
        builder.Property(n => n.Importance).HasDefaultValue(0.5f);
        builder.Property(n => n.ConfidenceScore).HasDefaultValue(1.0f);
        builder.Property(n => n.RetrievalCount).HasDefaultValue(0);
        builder.Property(n => n.IsVerified).HasDefaultValue(false);
        builder.Property(n => n.DiscoveredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        // Indexes for common queries
        builder.HasIndex(n => n.Title);
        builder.HasIndex(n => n.Type);
        builder.HasIndex(n => n.Tags);
        builder.HasIndex(n => n.DiscoveredAt);
        builder.HasIndex(n => n.Importance);
        
        // Full text search index (PostgreSQL specific)
        builder.HasIndex(n => n.Content)
            .HasMethod("GIN")
            .HasOperators("gin_trgm_ops");
            
        // Relationships
        builder.HasOne(n => n.Entity)
            .WithMany()
            .HasForeignKey(n => n.EntityId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
            
        builder.HasOne(n => n.VectorIndex)
            .WithMany()
            .HasForeignKey(n => n.VectorIndexId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
} 