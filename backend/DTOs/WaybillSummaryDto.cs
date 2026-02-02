using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for comprehensive waybill summary statistics.
/// 
/// PURPOSE:
/// This DTO provides aggregated statistics and analytics for waybills, enabling
/// business intelligence and reporting. It includes totals by status, monthly
/// breakdowns, top suppliers, project totals, and quality metrics.
/// 
/// USAGE:
/// This DTO is returned by GET /api/waybills/summary endpoint. It provides a
/// comprehensive view of waybill activity for dashboards, reports, and analytics.
/// 
/// DATE RANGE FILTERING:
/// All statistics are calculated for waybills within the optional date range.
/// If no date range is provided, all waybills for the tenant are included.
/// The date range is applied to the waybill_date field.
/// 
/// TENANT ISOLATION:
/// All statistics are automatically scoped to the current tenant. The global query
/// filter ensures that only waybills belonging to the tenant are included in
/// the calculations.
/// 
/// PERFORMANCE CONSIDERATIONS:
/// This endpoint performs multiple aggregations on potentially large datasets.
/// For optimal performance:
/// - Use date range filters to limit the dataset
/// - Database indexes on TenantId, Status, WaybillDate, ProjectId, SupplierId
/// - Consider caching results for frequently accessed date ranges
/// - Monitor query execution time for large datasets
/// </summary>
public class WaybillSummaryDto
{
    /// <summary>
    /// Total quantity of goods grouped by waybill status.
    /// 
    /// This dictionary provides a breakdown of total quantity by status:
    /// - Key: WaybillStatus enum value (Pending, Delivered, Cancelled, Disputed)
    /// - Value: Sum of all Quantity values for waybills with that status
    /// 
    /// EXAMPLE:
    /// {
    ///   "Pending": 150.5,
    ///   "Delivered": 1200.75,
    ///   "Cancelled": 50.0,
    ///   "Disputed": 25.25
    /// }
    /// 
    /// This helps identify the volume of goods in each status category.
    /// </summary>
    public Dictionary<WaybillStatus, decimal> TotalQuantityByStatus { get; set; } = new Dictionary<WaybillStatus, decimal>();

    /// <summary>
    /// Total amount (in currency) grouped by waybill status.
    /// 
    /// This dictionary provides a breakdown of total financial value by status:
    /// - Key: WaybillStatus enum value (Pending, Delivered, Cancelled, Disputed)
    /// - Value: Sum of all TotalAmount values for waybills with that status
    /// 
    /// EXAMPLE:
    /// {
    ///   "Pending": 15000.50,
    ///   "Delivered": 120000.75,
    ///   "Cancelled": 5000.00,
    ///   "Disputed": 2500.25
    /// }
    /// 
    /// This helps identify the financial value at risk in each status category.
    /// </summary>
    public Dictionary<WaybillStatus, decimal> TotalAmountByStatus { get; set; } = new Dictionary<WaybillStatus, decimal>();

    /// <summary>
    /// Monthly breakdown of waybill activity.
    /// 
    /// This list provides a time-series view of waybill activity, grouped by
    /// year and month. Each entry contains:
    /// - Year and Month
    /// - TotalQuantity for that month
    /// - TotalAmount for that month
    /// - DeliveryCount for that month
    /// 
    /// The list is ordered chronologically (oldest to newest).
    /// 
    /// This enables trend analysis and period-over-period comparisons.
    /// </summary>
    public List<MonthlySummaryDto> MonthlyBreakdown { get; set; } = new List<MonthlySummaryDto>();

    /// <summary>
    /// Top suppliers by delivery volume.
    /// 
    /// This list contains the top 10 suppliers ordered by TotalAmount (descending).
    /// Each entry contains:
    /// - SupplierId and SupplierName
    /// - TotalQuantity from this supplier
    /// - TotalAmount from this supplier
    /// - DeliveryCount from this supplier
    /// 
    /// This helps identify the most important suppliers by financial volume.
    /// </summary>
    public List<SupplierSummaryDto> TopSuppliers { get; set; } = new List<SupplierSummaryDto>();

    /// <summary>
    /// Project-level totals for all projects.
    /// 
    /// This list contains aggregated statistics for each project that has waybills.
    /// Each entry contains:
    /// - ProjectId and ProjectName
    /// - TotalQuantity to this project
    /// - TotalAmount to this project
    /// - DeliveryCount to this project
    /// 
    /// The list is ordered by TotalAmount (descending).
    /// 
    /// This helps identify the most active projects by financial volume.
    /// </summary>
    public List<ProjectSummaryDto> ProjectTotals { get; set; } = new List<ProjectSummaryDto>();

    /// <summary>
    /// Count of waybills with Disputed status.
    /// 
    /// This represents the number of waybills that have issues or disputes.
    /// A high DisputedCount may indicate quality or delivery problems.
    /// </summary>
    public int DisputedCount { get; set; }

    /// <summary>
    /// Count of waybills with Cancelled status.
    /// 
    /// This represents the number of waybills that were cancelled.
    /// A high CancelledCount may indicate planning or execution issues.
    /// </summary>
    public int CancelledCount { get; set; }

    /// <summary>
    /// Percentage of waybills that are disputed.
    /// 
    /// Calculated as: (DisputedCount / TotalWaybillCount) * 100
    /// 
    /// This metric helps assess delivery quality. A high percentage may indicate
    /// systemic issues that need attention.
    /// 
    /// Range: 0.0 to 100.0
    /// </summary>
    public decimal DisputedPercentage { get; set; }

    /// <summary>
    /// Percentage of waybills that are cancelled.
    /// 
    /// Calculated as: (CancelledCount / TotalWaybillCount) * 100
    /// 
    /// This metric helps assess planning accuracy. A high percentage may indicate
    /// issues with demand forecasting or order management.
    /// 
    /// Range: 0.0 to 100.0
    /// </summary>
    public decimal CancelledPercentage { get; set; }
}
