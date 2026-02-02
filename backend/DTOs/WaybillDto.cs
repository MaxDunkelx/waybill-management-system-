using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for waybill responses.
/// 
/// PURPOSE:
/// This DTO represents a complete waybill entity in API responses. It includes
/// all waybill fields plus related entity names (ProjectName, SupplierName) for
/// easier consumption by frontend applications.
/// 
/// USAGE:
/// This DTO is used in GET endpoints to return waybill data. It provides a
/// complete view of the waybill including related entity information without
/// requiring additional API calls.
/// 
/// HEBREW TEXT SUPPORT:
/// All text fields (ProductName, Unit, DriverName, DeliveryAddress, Notes, ProjectName, SupplierName)
/// support Hebrew Unicode characters and will be properly encoded in JSON responses
/// due to UTF-8 encoding configuration in Program.cs.
/// </summary>
public class WaybillDto
{
    /// <summary>
    /// Unique waybill identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Date when the waybill was issued.
    /// </summary>
    public DateTime WaybillDate { get; set; }

    /// <summary>
    /// Date when the goods were or will be delivered.
    /// </summary>
    public DateTime DeliveryDate { get; set; }

    /// <summary>
    /// Project identifier receiving this delivery.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Project name (Hebrew) for display purposes.
    /// This is included to avoid requiring a separate API call to get project details.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier identifier providing the goods.
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name (Hebrew) for display purposes.
    /// This is included to avoid requiring a separate API call to get supplier details.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Product code identifier.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Product name in Hebrew.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of goods delivered.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measurement in Hebrew (e.g., "מ\"ק", "ק\"ג").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total amount for the waybill.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., "ILS").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the waybill.
    /// </summary>
    public WaybillStatus Status { get; set; }

    /// <summary>
    /// Vehicle license plate number (optional).
    /// </summary>
    public string? VehicleNumber { get; set; }

    /// <summary>
    /// Driver name in Hebrew (optional).
    /// </summary>
    public string? DriverName { get; set; }

    /// <summary>
    /// Delivery address in Hebrew.
    /// </summary>
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes in Hebrew (optional).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp when the waybill was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the waybill was last updated (null if never updated).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
