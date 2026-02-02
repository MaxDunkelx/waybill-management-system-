using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for supplier summary statistics.
/// 
/// PURPOSE:
/// This DTO provides comprehensive statistics about a supplier's waybill activity,
/// including totals, averages, and status breakdown. It's used for supplier
/// performance analysis and reporting.
/// 
/// USAGE:
/// This DTO is returned by GET /api/suppliers/{id}/summary endpoint to provide
/// insights into a supplier's delivery activity, financial totals, and waybill
/// status distribution.
/// 
/// CALCULATIONS:
/// - TotalDeliveries: Count of all waybills from this supplier
/// - TotalQuantity: Sum of all quantities from this supplier
/// - TotalAmount: Sum of all total amounts from this supplier
/// - AverageQuantityPerDelivery: TotalQuantity / TotalDeliveries
/// - StatusBreakdown: Count of waybills grouped by status
/// 
/// TENANT ISOLATION:
/// All statistics are automatically scoped to the current tenant. The global query
/// filter ensures that only waybills belonging to the tenant are included in the
/// calculations. Cross-tenant data access is prevented.
/// 
/// </summary>
public class SupplierSummaryResponseDto
{
    /// <summary>
    /// Unique identifier for the supplier.
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the supplier (Hebrew).
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of waybills (deliveries) from this supplier.
    /// This count includes all waybills regardless of status.
    /// </summary>
    public int TotalDeliveries { get; set; }

    /// <summary>
    /// Total quantity of goods delivered by this supplier.
    /// This is the sum of all Quantity values for waybills from this supplier.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount (in currency) for waybills from this supplier.
    /// This is the sum of all TotalAmount values for waybills from this supplier.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Average quantity per delivery.
    /// Calculated as: TotalQuantity / TotalDeliveries
    /// Returns 0 if TotalDeliveries is 0 (to avoid division by zero).
    /// </summary>
    public decimal AverageQuantityPerDelivery { get; set; }

    /// <summary>
    /// Breakdown of waybills by status.
    /// 
    /// This dictionary provides a count of waybills for each status:
    /// - Key: WaybillStatus enum value (Pending, Delivered, Cancelled, Disputed)
    /// - Value: Count of waybills with that status
    /// 
    /// EXAMPLE:
    /// {
    ///   "Pending": 5,
    ///   "Delivered": 20,
    ///   "Cancelled": 2,
    ///   "Disputed": 1
    /// }
    /// 
    /// This helps identify the distribution of waybill statuses for the supplier,
    /// which can indicate supplier performance and reliability.
    /// </summary>
    public Dictionary<WaybillStatus, int> StatusBreakdown { get; set; } = new Dictionary<WaybillStatus, int>();
}
