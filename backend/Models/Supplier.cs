using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaybillManagementSystem.Models;

/// <summary>
/// Represents a supplier/vendor in the Waybill Management System.
/// Suppliers provide goods and services that are tracked through waybills.
/// Suppliers can be tenant-specific (each tenant has their own supplier list) or
/// shared across tenants, depending on business requirements. The TenantId field
/// supports both scenarios - it can be set to a specific tenant ID or left as
/// a shared identifier for multi-tenant suppliers.
/// </summary>
public class Supplier
{
    /// <summary>
    /// Unique supplier identifier, typically in a format like "SUP001", "SUP002", etc.
    /// This is part of the composite primary key (TenantId, Id) and is used as a foreign key in Waybill entities.
    /// The format allows for human-readable supplier codes while maintaining uniqueness within a tenant.
    /// 
    /// COMPOSITE PRIMARY KEY:
    /// The primary key is (TenantId, Id), which allows multiple tenants to have suppliers with the same ID.
    /// For example, both TENANT001 and TENANT002 can have a supplier with ID "SUP003" (e.g., "תרמיקס ישראל").
    /// This is realistic because multiple construction companies often use the same suppliers.
    /// 
    /// TENANT ISOLATION:
    /// Even though multiple tenants can have suppliers with the same ID, tenant isolation is maintained
    /// through global query filters that automatically filter by TenantId in all queries.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the supplier, typically in Hebrew.
    /// This name is displayed in the UI and on waybill documents.
    /// Stored as nvarchar in SQL Server to support Hebrew Unicode characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key reference to the Tenant that owns or uses this supplier.
    /// This field is part of the composite primary key (TenantId, Id), ensuring that each tenant
    /// can have their own supplier records even if they share the same supplier ID.
    /// 
    /// COMPOSITE PRIMARY KEY:
    /// The primary key is (TenantId, Id), which allows:
    /// - TENANT001 to have supplier (TENANT001, SUP003) = "תרמיקס ישראל"
    /// - TENANT002 to have supplier (TENANT002, SUP003) = "תרמיקס ישראל"
    /// Both are stored as separate records in the database.
    /// 
    /// TENANT ISOLATION:
    /// The global query filter in ApplicationDbContext automatically filters suppliers by TenantId,
    /// ensuring tenants only see their own suppliers. Even if two tenants have suppliers with the
    /// same ID, they cannot see each other's supplier records or waybills.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp indicating when the supplier was created in the system.
    /// Useful for auditing and tracking supplier lifecycle.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the Tenant that owns or uses this supplier.
    /// This relationship is enforced at the database level through the TenantId foreign key.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation property to all waybills from this supplier.
    /// This collection allows easy access to all deliveries from a specific supplier.
    /// </summary>
    public virtual ICollection<Waybill> Waybills { get; set; } = new List<Waybill>();
}
