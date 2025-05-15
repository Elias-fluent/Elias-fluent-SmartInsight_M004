using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Application role entity that extends ASP.NET Identity's IdentityRole
/// with tenant-specific properties
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public ApplicationRole() : base() { }

    /// <summary>
    /// Constructor with role name
    /// </summary>
    /// <param name="roleName">Name of the role</param>
    public ApplicationRole(string roleName) : base(roleName) { }

    /// <summary>
    /// Description of the role
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tenant ID this role belongs to
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Whether this role is a system role that cannot be deleted
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the role
    /// </summary>
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the role was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the role
    /// </summary>
    [MaxLength(256)]
    public string? UpdatedBy { get; set; }
} 