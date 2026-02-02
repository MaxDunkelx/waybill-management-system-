using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for waybill query parameters.
/// 
/// PURPOSE:
/// This DTO represents query parameters for filtering and paginating waybill lists.
/// It allows clients to filter waybills by various criteria and retrieve paginated results.
/// 
/// USAGE:
/// This DTO is used as query parameters in GET /api/waybills endpoint. All parameters
/// are optional, allowing flexible querying. If no parameters are provided, all waybills
/// for the current tenant are returned (with pagination).
/// 
/// FILTERING:
/// Multiple filters can be combined using AND logic:
/// - Date range: Filter by waybill_date or delivery_date
/// - Status: Filter by waybill status
/// - Project: Filter by project ID
/// - Supplier: Filter by supplier ID
/// - Product: Filter by product code
/// - Text search: Search in project name, supplier name (Hebrew-aware)
/// 
/// PAGINATION:
/// - PageNumber: 1-based page number (default: 1)
/// - PageSize: Number of items per page (default: 20, max: 100)
/// 
/// HEBREW TEXT SEARCH:
/// The SearchText parameter searches in:
/// - Project name (Hebrew)
/// - Supplier name (Hebrew)
/// - Product name (Hebrew)
/// 
/// The search is case-insensitive and uses SQL Server's Unicode-aware string comparison,
/// which properly handles Hebrew characters. The search uses LIKE pattern matching.
/// 
/// PERFORMANCE:
/// All filters are applied at the database level using IQueryable, ensuring efficient
/// query execution. Indexes on TenantId, Status, WaybillDate, ProjectId, and SupplierId
/// optimize filter performance.
/// 
/// EXAMPLE:
/// ?status=Delivered&dateFrom=2024-01-01&dateTo=2024-01-31&pageNumber=1&pageSize=20
/// </summary>
[SwaggerSchema(Description = "Query parameters for filtering and paginating waybill lists")]
public class WaybillQueryDto
{
    /// <summary>
    /// Filter waybills by waybill date from (inclusive).
    /// Format: YYYY-MM-DD
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Filter waybills by waybill date to (inclusive).
    /// Format: YYYY-MM-DD
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Filter waybills by delivery date from (inclusive).
    /// Format: YYYY-MM-DD
    /// </summary>
    public DateTime? DeliveryDateFrom { get; set; }

    /// <summary>
    /// Filter waybills by delivery date to (inclusive).
    /// Format: YYYY-MM-DD
    /// </summary>
    public DateTime? DeliveryDateTo { get; set; }

    /// <summary>
    /// Filter waybills by status.
    /// Valid values: Pending, Delivered, Cancelled, Disputed
    /// </summary>
    public WaybillStatus? Status { get; set; }

    /// <summary>
    /// Filter waybills by project ID.
    /// Only waybills for the specified project are returned.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Filter waybills by supplier ID.
    /// Only waybills from the specified supplier are returned.
    /// </summary>
    public string? SupplierId { get; set; }

    /// <summary>
    /// Filter waybills by product code.
    /// Exact match on product code.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Text search in project name, supplier name, and product name.
    /// 
    /// HEBREW TEXT SEARCH:
    /// This parameter performs a case-insensitive search in:
    /// - Project name (Hebrew)
    /// - Supplier name (Hebrew)
    /// - Product name (Hebrew)
    /// 
    /// The search uses SQL Server's Unicode-aware string comparison, which properly
    /// handles Hebrew characters. The search pattern uses LIKE with wildcards:
    /// - Input: "בטון" → SQL: "%בטון%"
    /// 
    /// This allows partial matches in Hebrew text, making it easy to find waybills
    /// by searching for part of a project, supplier, or product name.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// Default: 1
    /// Minimum: 1
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// Default: 20
    /// Minimum: 1
    /// Maximum: 100 (to prevent excessive data transfer)
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
}
