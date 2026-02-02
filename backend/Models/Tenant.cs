using System.ComponentModel.DataAnnotations;

namespace WaybillManagementSystem.Models;

/// <summary>
/// Represents a tenant in the multi-tenant Waybill Management System.
/// Tenants are isolated organizations that have their own waybills, projects, and suppliers.
/// Each tenant's data is completely isolated from other tenants through the global query filter
/// in ApplicationDbContext, ensuring data security and privacy at the database level.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Unique identifier for the tenant. This is the primary key and is used
    /// as the TenantId foreign key in all related entities (Waybills, Projects, Suppliers).
    /// Typically a GUID or a unique string identifier.
    /// </summary>
    [Key]
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the tenant organization.
    /// This name is used in the UI and reports to identify the tenant.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp indicating when the tenant was created in the system.
    /// This is automatically set when the tenant is first created and is useful
    /// for auditing and tracking tenant lifecycle.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to all waybills belonging to this tenant.
    /// This collection is automatically filtered by the global query filter
    /// in ApplicationDbContext, ensuring only this tenant's waybills are accessible.
    /// </summary>
    public virtual ICollection<Waybill> Waybills { get; set; } = new List<Waybill>();

    /// <summary>
    /// Navigation property to all projects belonging to this tenant.
    /// Projects are tenant-specific and cannot be shared across tenants.
    /// </summary>
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    /// <summary>
    /// Navigation property to all suppliers belonging to this tenant.
    /// Suppliers can be tenant-specific or shared, depending on business requirements.
    /// </summary>
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
