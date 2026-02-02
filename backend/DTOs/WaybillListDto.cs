using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for waybill list responses.
/// 
/// PURPOSE:
/// This DTO represents a simplified waybill entity for list views. It includes
/// only the most important fields needed for displaying waybills in a table or list,
/// reducing payload size and improving performance.
/// 
/// USAGE:
/// This DTO is used in GET /api/waybills endpoint for list views. For detailed
/// waybill information, use GET /api/waybills/{id} which returns the full WaybillDto.
/// 
/// FIELDS INCLUDED:
/// - Essential identification: Id, WaybillDate, DeliveryDate
/// - Key relationships: ProjectName, SupplierName (for display)
/// - Product information: ProductCode, ProductName
/// - Financial summary: TotalAmount, Currency
/// - Status: Current waybill status
/// 
/// FIELDS EXCLUDED (to reduce payload):
/// - Detailed pricing breakdown (UnitPrice, Quantity, Unit)
/// - Delivery details (VehicleNumber, DriverName, DeliveryAddress)
/// - Notes
/// - Timestamps (CreatedAt, UpdatedAt)
/// 
/// These excluded fields are available in the full WaybillDto when needed.
/// </summary>
public class WaybillListDto
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
    /// Project name (Hebrew) receiving this delivery.
    /// Included for display purposes without requiring additional API calls.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name (Hebrew) providing the goods.
    /// Included for display purposes without requiring additional API calls.
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
}
