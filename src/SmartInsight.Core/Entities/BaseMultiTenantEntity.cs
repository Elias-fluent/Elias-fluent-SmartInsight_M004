using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Base class for all multi-tenant entities, ensuring tenant isolation
/// </summary>
public abstract class BaseMultiTenantEntity : BaseEntity
{
    /// <summary>
    /// The tenant ID this entity belongs to, ensuring proper data isolation
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Navigation property to the Tenant
    /// </summary>
    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }
} 