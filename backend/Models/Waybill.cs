using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Models;

/// <summary>
/// Represents a waybill (delivery note) in the Waybill Management System.
/// This is the core entity that tracks deliveries of goods from suppliers to projects.
/// Each waybill contains detailed information about the product, quantity, pricing,
/// delivery details, and status. Waybills are tenant-isolated and cannot be accessed
/// across tenant boundaries due to the global query filter in ApplicationDbContext.
/// </summary>
public class Waybill
{
    /// <summary>
    /// Unique waybill identifier, typically from the CSV import (waybill_id field).
    /// This is the primary key and uniquely identifies each waybill in the system.
    /// The ID format depends on the source system but must be unique within a tenant.
    /// </summary>
    [Key]
    [Required]
    [StringLength(100)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The date when the waybill was issued or created.
    /// This is typically the date from the original waybill document.
    /// </summary>
    [Required]
    [Column(TypeName = "date")]
    public DateTime WaybillDate { get; set; }

    /// <summary>
    /// The date when the goods were or are scheduled to be delivered.
    /// This is the actual or planned delivery date to the project site.
    /// </summary>
    [Required]
    [Column(TypeName = "date")]
    public DateTime DeliveryDate { get; set; }

    /// <summary>
    /// Foreign key reference to the Project receiving this delivery.
    /// This field is required as every waybill must be associated with a project.
    /// The global query filter ensures that only projects belonging to the same tenant
    /// can be referenced, maintaining tenant isolation.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key reference to the Supplier providing the goods.
    /// This field is required as every waybill must have a supplier.
    /// The global query filter ensures that only suppliers belonging to the same tenant
    /// (or shared suppliers) can be referenced.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Product code identifier, typically a short code like "B30", "C25", etc.
    /// This code is used to identify the type of product being delivered.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// The name of the product being delivered, typically in Hebrew.
    /// This is a human-readable description of the product.
    /// Stored as nvarchar in SQL Server to support Hebrew Unicode characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The quantity of goods delivered, stored as a decimal to support
    /// fractional quantities (e.g., 2.5 cubic meters).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// The unit of measurement for the quantity, typically in Hebrew.
    /// Examples: "מ\"ק" (cubic meters), "ק\"ג" (kilograms), "יח'" (units).
    /// Stored as nvarchar to support Hebrew Unicode characters.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// The price per unit of the product.
    /// Stored as decimal(18,2) to support currency precision (2 decimal places).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// The total amount for this waybill (Quantity × UnitPrice).
    /// This is calculated and stored to avoid recalculation on every query.
    /// Stored as decimal(18,2) to support currency precision.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// The currency code for the pricing, typically "ILS" (Israeli Shekel).
    /// This field supports multi-currency scenarios if needed in the future.
    /// </summary>
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "ILS";

    /// <summary>
    /// The current status of the waybill (Pending, Delivered, Cancelled, or Disputed).
    /// This enum field tracks the lifecycle of the waybill and determines
    /// which operations are allowed (e.g., only Pending waybills can be cancelled).
    /// </summary>
    [Required]
    public WaybillStatus Status { get; set; } = WaybillStatus.Pending;

    /// <summary>
    /// The license plate number of the delivery vehicle.
    /// This field is nullable because not all waybills may have vehicle information,
    /// especially for early-stage entries or manual waybill creation.
    /// </summary>
    [StringLength(20)]
    public string? VehicleNumber { get; set; }

    /// <summary>
    /// The name of the driver delivering the goods, typically in Hebrew.
    /// This field is nullable because driver information may not always be available
    /// at the time of waybill creation.
    /// Stored as nvarchar to support Hebrew Unicode characters.
    /// </summary>
    [StringLength(200)]
    public string? DriverName { get; set; }

    /// <summary>
    /// The delivery address where the goods should be delivered, typically in Hebrew.
    /// This is the physical location of the project site or delivery point.
    /// Stored as nvarchar to support Hebrew Unicode characters.
    /// </summary>
    [StringLength(1000)]
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes or comments about the waybill, typically in Hebrew.
    /// This field is nullable and can contain any relevant information that doesn't
    /// fit into the structured fields (e.g., special instructions, delivery conditions).
    /// Stored as nvarchar to support Hebrew Unicode characters.
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Foreign key reference to the Tenant that owns this waybill.
    /// This field is required and is critical for multi-tenant data isolation.
    /// The global query filter in ApplicationDbContext automatically filters all
    /// waybill queries by this TenantId, ensuring tenants can only access their own data.
    /// This is the primary mechanism for tenant isolation at the database level.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp indicating when the waybill was first created in the system.
    /// This is automatically set when the waybill is created and is useful for auditing.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp indicating when the waybill was last updated.
    /// This field is nullable because newly created waybills haven't been updated yet.
    /// It's automatically updated whenever the waybill is modified, enabling change tracking.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Row version used for optimistic concurrency control.
    /// This byte array is automatically updated by SQL Server whenever the row is modified.
    /// When updating a waybill, EF Core compares this value to detect concurrent modifications
    /// and throws a DbUpdateConcurrencyException if another user has modified the record
    /// since it was loaded. This prevents lost updates in multi-user scenarios.
    /// 
    /// WHY OPTIMISTIC LOCKING: In a multi-tenant system with multiple users, two users might
    /// try to update the same waybill simultaneously. Without optimistic locking, the last
    /// save would overwrite the first user's changes. With this mechanism, the second user
    /// gets an error and must reload the latest data before making changes.
    /// </summary>
    [Timestamp]
    public byte[]? Version { get; set; }

    /// <summary>
    /// ERP synchronization status for Priority ERP integration.
    /// 
    /// This field tracks whether the waybill has been successfully synchronized with the
    /// external Priority ERP system. The background ERP sync service processes waybills
    /// with PENDING_SYNC status and updates this field based on sync results.
    /// 
    /// STATUS VALUES:
    /// - PENDING_SYNC: Waybill has not yet been sent to ERP (initial state)
    /// - SYNCED: Waybill has been successfully synchronized with ERP
    /// - SYNC_FAILED: Waybill synchronization failed after all retry attempts
    /// 
    /// DEFAULT VALUE:
    /// New waybills start with PENDING_SYNC status and are automatically processed
    /// by the background ERP sync service.
    /// </summary>
    public Enums.ErpSyncStatus ErpSyncStatus { get; set; } = Enums.ErpSyncStatus.PendingSync;

    /// <summary>
    /// Timestamp of the last ERP synchronization attempt.
    /// 
    /// This field is updated whenever a sync attempt is made, regardless of success or failure.
    /// It helps track when waybills were last processed and can be used for monitoring and
    /// debugging sync issues.
    /// </summary>
    public DateTime? LastErpSyncAttemptAt { get; set; }

    /// <summary>
    /// Navigation property to the Tenant that owns this waybill.
    /// This relationship is enforced at the database level through the TenantId foreign key.
    /// The global query filter ensures this navigation property only returns the correct tenant.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation property to the Project receiving this delivery.
    /// This relationship is enforced at the database level through the ProjectId foreign key.
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation property to the Supplier providing the goods.
    /// This relationship is enforced at the database level through the SupplierId foreign key.
    /// </summary>
    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;
}
