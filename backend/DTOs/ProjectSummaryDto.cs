namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for project-level waybill summary statistics.
/// 
/// PURPOSE:
/// This DTO represents aggregated waybill statistics for a specific project.
/// It provides insights into project activity, including total volume, financial
/// totals, and delivery frequency.
/// 
/// USAGE:
/// This DTO is used in WaybillSummaryDto.ProjectTotals to provide a breakdown
/// of waybill activity by project. This helps identify which projects are most
/// active and have the highest delivery volumes.
/// 
/// CALCULATIONS:
/// - TotalQuantity: Sum of all quantities for waybills to this project
/// - TotalAmount: Sum of all total amounts for waybills to this project
/// - DeliveryCount: Count of waybills to this project
/// 
/// TENANT ISOLATION:
/// Project summaries are automatically scoped to the current tenant. Only waybills
/// belonging to the tenant are included in the calculations.
/// 
/// DATE RANGE:
/// The project summary includes only waybills within the specified date range
/// (if provided). The date range is applied to the waybill_date field.
/// </summary>
public class ProjectSummaryDto
{
    /// <summary>
    /// Unique identifier for the project.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the project (Hebrew).
    /// This is included for display purposes without requiring additional API calls.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Total quantity of goods delivered to this project.
    /// This is the sum of all Quantity values for waybills to this project.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount (in currency) for waybills to this project.
    /// This is the sum of all TotalAmount values for waybills to this project.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of waybills to this project.
    /// This count includes all waybills regardless of status.
    /// </summary>
    public int DeliveryCount { get; set; }
}
