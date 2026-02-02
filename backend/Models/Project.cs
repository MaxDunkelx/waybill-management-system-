using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaybillManagementSystem.Models;

/// <summary>
/// Represents a project within a tenant's organization.
/// Projects are used to group waybills and track deliveries for specific construction
/// or business projects. Each project belongs to a single tenant and cannot be shared
/// across tenants, ensuring complete data isolation.
/// </summary>
public class Project
{
    /// <summary>
    /// Unique project identifier, typically in a format like "PRJ001", "PRJ002", etc.
    /// This is the primary key and is used as a foreign key in Waybill entities.
    /// The format allows for human-readable project codes while maintaining uniqueness.
    /// </summary>
    [Key]
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the project, typically in Hebrew.
    /// This name is displayed in the UI and on waybill documents.
    /// Stored as nvarchar in SQL Server to support Hebrew Unicode characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key reference to the Tenant that owns this project.
    /// This field is required and ensures that every project belongs to exactly one tenant.
    /// The global query filter in ApplicationDbContext automatically filters projects by this TenantId,
    /// preventing cross-tenant data access.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp indicating when the project was created in the system.
    /// Useful for auditing and tracking project lifecycle.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the Tenant that owns this project.
    /// This relationship is enforced at the database level through the TenantId foreign key.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation property to all waybills associated with this project.
    /// This collection allows easy access to all deliveries for a specific project.
    /// </summary>
    public virtual ICollection<Waybill> Waybills { get; set; } = new List<Waybill>();
}
