using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for updating waybill data.
/// 
/// PURPOSE:
/// This DTO represents a request to update a waybill's data. It includes all updatable
/// fields plus the Version property for optimistic locking.
/// 
/// OPTIMISTIC LOCKING:
/// The Version property is critical for optimistic locking. It must contain the current
/// version of the waybill as known by the client. When the update is processed:
/// 1. The server loads the waybill from the database
/// 2. Compares the client's Version with the database Version
/// 3. If they match: Update proceeds and Version is automatically incremented
/// 4. If they don't match: Update is rejected with 409 Conflict (concurrency conflict)
/// 
/// CLIENT-SIDE IMPLEMENTATION:
/// When a client loads a waybill for editing:
/// 1. Store the Version value from the GET response
/// 2. Include this Version in the PUT request
/// 3. If update fails with 409 Conflict:
///    - Refresh the waybill data (GET request)
///    - Show user that another user modified it
///    - Allow user to review changes and update again
/// 
/// VERSION PROPERTY:
/// The Version is a byte array (rowversion in SQL Server) that is automatically
/// updated by the database on each update. EF Core handles the version checking
/// automatically when the property is configured with [Timestamp] attribute.
/// 
/// EXAMPLE:
/// {
///   "version": "base64-encoded-version-from-get-response",
///   "waybillDate": "2024-01-15",
///   "deliveryDate": "2024-01-20",
///   "projectId": "PRJ001",
///   "supplierId": "SUP001",
///   "productCode": "B30",
///   "productName": "בטון ב-30",
///   "quantity": 10.5,
///   "unit": "מ\"ק",
///   "unitPrice": 150.75,
///   "totalAmount": 1582.87,
///   "currency": "ILS",
///   "status": "Delivered",
///   "vehicleNumber": "123-45-678",
///   "driverName": "יוסי כהן",
///   "deliveryAddress": "רחוב הרצל 1, תל אביב",
///   "notes": "Additional notes"
/// }
/// </summary>
[SwaggerSchema(Description = "Request to update a waybill. Must include Version for optimistic locking.")]
public class UpdateWaybillDto
{
    /// <summary>
    /// Current version of the waybill (for optimistic locking).
    /// 
    /// This must be the Version value from the most recent GET request for this waybill.
    /// The server will compare this with the database version to detect concurrent updates.
    /// 
    /// REQUIRED: Yes - optimistic locking requires version checking
    /// </summary>
    [Required(ErrorMessage = "Version is required for optimistic locking")]
    public byte[] Version { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Date when the waybill was issued.
    /// </summary>
    [Required]
    public DateTime WaybillDate { get; set; }

    /// <summary>
    /// Date when the goods were or will be delivered.
    /// </summary>
    [Required]
    public DateTime DeliveryDate { get; set; }

    /// <summary>
    /// Project identifier receiving this delivery.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Supplier identifier providing the goods.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Product code identifier.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Product name in Hebrew.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of goods delivered.
    /// </summary>
    [Required]
    [Range(0.5, 50, ErrorMessage = "Quantity must be between 0.5 and 50")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measurement in Hebrew (e.g., "מ\"ק", "ק\"ג").
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit.
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be positive")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total amount for the waybill.
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Total amount must be positive")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., "ILS").
    /// </summary>
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "ILS";

    /// <summary>
    /// Current status of the waybill.
    /// </summary>
    [Required]
    public WaybillStatus Status { get; set; }

    /// <summary>
    /// Vehicle license plate number (optional).
    /// </summary>
    [StringLength(20)]
    public string? VehicleNumber { get; set; }

    /// <summary>
    /// Driver name in Hebrew (optional).
    /// </summary>
    [StringLength(200)]
    public string? DriverName { get; set; }

    /// <summary>
    /// Delivery address in Hebrew.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes in Hebrew (optional).
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }
}
