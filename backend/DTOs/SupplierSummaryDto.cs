namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for supplier-level waybill summary statistics.
/// 
/// PURPOSE:
/// This DTO represents aggregated waybill statistics for a specific supplier.
/// It provides insights into supplier performance, including total volume, financial
/// totals, and delivery frequency.
/// 
/// USAGE:
/// This DTO is used in WaybillSummaryDto.TopSuppliers to identify the most active
/// suppliers by volume. The list is ordered by TotalAmount (descending) and limited
/// to the top 10 suppliers.
/// 
/// CALCULATIONS:
/// - TotalQuantity: Sum of all quantities for waybills from this supplier
/// - TotalAmount: Sum of all total amounts for waybills from this supplier
/// - DeliveryCount: Count of waybills from this supplier
/// 
/// TENANT ISOLATION:
/// Supplier summaries are automatically scoped to the current tenant. Only waybills
/// belonging to the tenant are included in the calculations.
/// 
/// DATE RANGE:
/// The supplier summary includes only waybills within the specified date range
/// (if provided). The date range is applied to the waybill_date field.
/// </summary>
public class SupplierSummaryDto
{
    /// <summary>
    /// Unique identifier for the supplier.
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the supplier (Hebrew).
    /// This is included for display purposes without requiring additional API calls.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

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
    /// Number of waybills from this supplier.
    /// This count includes all waybills regardless of status.
    /// </summary>
    public int DeliveryCount { get; set; }
}
