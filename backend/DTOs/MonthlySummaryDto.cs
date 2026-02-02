namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for monthly waybill summary statistics.
/// 
/// PURPOSE:
/// This DTO represents aggregated waybill statistics for a specific month.
/// It provides insights into delivery volume and financial totals for each month,
/// allowing for trend analysis and period-over-period comparisons.
/// 
/// USAGE:
/// This DTO is used in WaybillSummaryDto.MonthlyBreakdown to provide a time-series
/// view of waybill activity. The data is grouped by year and month, ordered chronologically.
/// 
/// CALCULATIONS:
/// - TotalQuantity: Sum of all quantities for waybills in this month
/// - TotalAmount: Sum of all total amounts for waybills in this month
/// - DeliveryCount: Count of waybills in this month (regardless of status)
/// 
/// DATE RANGE:
/// The monthly breakdown includes only waybills within the specified date range
/// (if provided). The date range is applied to the waybill_date field.
/// </summary>
public class MonthlySummaryDto
{
    /// <summary>
    /// Year of the summary period (e.g., 2024).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the summary period (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Total quantity of goods delivered in this month.
    /// This is the sum of all Quantity values for waybills in this month.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount (in currency) for waybills in this month.
    /// This is the sum of all TotalAmount values for waybills in this month.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of waybills in this month.
    /// This count includes all waybills regardless of status (Pending, Delivered, Cancelled, Disputed).
    /// </summary>
    public int DeliveryCount { get; set; }
}
