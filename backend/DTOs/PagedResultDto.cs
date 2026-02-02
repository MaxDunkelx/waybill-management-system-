namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for paginated query results.
/// 
/// PURPOSE:
/// This DTO wraps paginated results, providing both the data items and pagination
/// metadata. This allows clients to implement pagination UI and understand the
/// total number of items available.
/// 
/// USAGE:
/// This DTO is used in GET endpoints that return paginated lists, such as
/// GET /api/waybills. It provides all information needed for pagination controls.
/// 
/// PAGINATION METADATA:
/// - TotalCount: Total number of items matching the query (across all pages)
/// - PageNumber: Current page number (1-based)
/// - PageSize: Number of items per page
/// - TotalPages: Total number of pages (calculated from TotalCount and PageSize)
/// - HasPreviousPage: Whether there is a previous page
/// - HasNextPage: Whether there is a next page
/// 
/// These properties allow clients to build pagination controls without additional
/// API calls.
/// </summary>
/// <typeparam name="T">The type of items in the result list.</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// List of items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Total number of items matching the query (across all pages).
    /// This count includes all items that match the filters, not just the current page.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// Calculated as: Ceiling(TotalCount divided by PageSize)
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// True if PageNumber > 1.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page.
    /// True if PageNumber < TotalPages.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
