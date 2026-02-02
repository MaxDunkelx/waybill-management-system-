using WaybillManagementSystem.DTOs;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for querying waybill data.
/// 
/// PURPOSE:
/// This service provides methods for retrieving waybill data with filtering,
/// pagination, and search capabilities. All queries automatically filter by tenant
/// through the global query filter in ApplicationDbContext.
/// 
/// TENANT ISOLATION:
/// All methods require a tenantId parameter, which is verified against the current
/// tenant context. The ApplicationDbContext's global query filter ensures that
/// all database queries are automatically scoped to the current tenant, providing
/// defense-in-depth security.
/// 
/// QUERY OPTIMIZATION:
/// Methods return IQueryable or use IQueryable internally to allow EF Core to
/// optimize queries at the database level. This ensures:
/// - Efficient SQL generation
/// - Proper use of database indexes
/// - Minimal data transfer
/// - Optimal performance for large datasets
/// </summary>
public interface IWaybillService
{
    /// <summary>
    /// Gets a waybill by its ID.
    /// 
    /// This method retrieves a single waybill with all related information
    /// (project name, supplier name) for the specified tenant.
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant are returned. Even if a waybill ID exists for another
    /// tenant, it will not be returned.
    /// 
    /// </summary>
    /// <param name="id">The waybill ID to retrieve.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>
    /// WaybillDto if found, null if not found or belongs to different tenant.
    /// </returns>
    Task<WaybillDto?> GetByIdAsync(string id, string tenantId);

    /// <summary>
    /// Gets a paginated list of waybills with optional filtering.
    /// 
    /// This method supports comprehensive filtering and pagination:
    /// - Date range filtering (waybill_date, delivery_date)
    /// - Status filtering
    /// - Project/Supplier/Product filtering
    /// - Hebrew text search
    /// - Pagination
    /// 
    /// FILTERING:
    /// Multiple filters can be combined using AND logic. All filters are applied
    /// at the database level for optimal performance.
    /// 
    /// PAGINATION:
    /// Results are paginated to prevent excessive data transfer. The method returns
    /// a PagedResultDto containing the current page of results and pagination metadata.
    /// 
    /// HEBREW TEXT SEARCH:
    /// The SearchText parameter searches in project name, supplier name, and product name
    /// using Unicode-aware string comparison, properly handling Hebrew characters.
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant are returned.
    /// 
    /// </summary>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <param name="query">Query parameters for filtering and pagination.</param>
    /// <returns>
    /// PagedResultDto containing the current page of waybills and pagination metadata.
    /// </returns>
    Task<PagedResultDto<WaybillListDto>> GetAllAsync(string tenantId, WaybillQueryDto query);

    /// <summary>
    /// Gets all waybills for a specific project.
    /// 
    /// This method retrieves all waybills associated with a project for the
    /// specified tenant. Useful for project-specific views and reports.
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that:
    /// 1. Only waybills belonging to the specified tenant are returned
    /// 2. Only projects belonging to the specified tenant can be queried
    /// 
    /// This prevents cross-tenant data access even if a project ID from another
    /// tenant is provided.
    /// 
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>
    /// List of waybills for the specified project, or empty list if project
    /// doesn't exist or belongs to different tenant.
    /// </returns>
    Task<List<WaybillListDto>> GetByProjectIdAsync(string projectId, string tenantId);

    /// <summary>
    /// Gets comprehensive summary statistics for waybills.
    /// 
    /// This method performs multiple aggregations to provide business intelligence
    /// and analytics. It includes:
    /// - Totals by status (quantity and amount)
    /// - Monthly breakdown of activity
    /// - Top suppliers by volume
    /// - Project-level totals
    /// - Quality metrics (disputed/cancelled percentages)
    /// 
    /// DATE RANGE FILTERING:
    /// If dateFrom and dateTo are provided, only waybills within that date range
    /// (based on waybill_date) are included in the calculations. This allows
    /// for period-specific analysis.
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant are included in the calculations.
    /// 
    /// PERFORMANCE:
    /// This method performs multiple database aggregations. For optimal performance:
    /// - Use date range filters to limit the dataset
    /// - Database indexes on TenantId, Status, WaybillDate optimize the queries
    /// - Consider caching results for frequently accessed date ranges
    /// 
    /// </summary>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <param name="dateFrom">Optional start date for filtering (inclusive).</param>
    /// <param name="dateTo">Optional end date for filtering (inclusive).</param>
    /// <returns>
    /// WaybillSummaryDto containing all aggregated statistics.
    /// </returns>
    Task<WaybillSummaryDto> GetSummaryAsync(string tenantId, DateTime? dateFrom, DateTime? dateTo);

    /// <summary>
    /// Updates the status of a waybill with business rule validation.
    /// 
    /// This method validates status transitions according to business rules:
    /// - PENDING → DELIVERED or CANCELLED (allowed)
    /// - DELIVERED → DISPUTED (allowed)
    /// - CANCELLED → anything (NOT allowed - no backward transitions)
    /// - Any other transition (NOT allowed)
    /// 
    /// STATUS TRANSITION RULES:
    /// The status transition must comply with the following rules:
    /// 1. PENDING waybills can be moved to DELIVERED (successful delivery) or CANCELLED (cancelled before delivery)
    /// 2. DELIVERED waybills can be moved to DISPUTED (if there are issues with the delivery)
    /// 3. CANCELLED waybills cannot be changed to any other status (final state)
    /// 4. DISPUTED waybills cannot be changed (must be resolved through other means)
    /// 5. Any other transition is not allowed
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant can be updated. Cross-tenant updates are prevented.
    /// 
    /// </summary>
    /// <param name="waybillId">The waybill ID to update.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <param name="dto">The status update request containing new status and optional notes.</param>
    /// <returns>
    /// Updated WaybillDto if successful.
    /// Throws InvalidOperationException if status transition is not allowed.
    /// Returns null if waybill not found or belongs to different tenant.
    /// </returns>
    Task<WaybillDto?> UpdateStatusAsync(string waybillId, string tenantId, UpdateWaybillStatusDto dto);

    /// <summary>
    /// Updates a waybill with optimistic locking to prevent concurrent update conflicts.
    /// 
    /// This method implements optimistic locking by checking the Version property:
    /// 1. Load waybill from database
    /// 2. Compare client's Version with database Version
    /// 3. If versions match: Update fields and save (EF Core automatically updates Version)
    /// 4. If versions don't match: Throw ConcurrencyException
    /// 
    /// OPTIMISTIC LOCKING:
    /// Optimistic locking prevents lost updates by detecting when another user has
    /// modified the entity since it was last read. The Version property (rowversion
    /// in SQL Server) is automatically updated by the database on each update.
    /// 
    /// HOW IT WORKS:
    /// - Client loads waybill (GET request) and receives Version value
    /// - Client modifies waybill data and sends PUT request with Version
    /// - Server compares client Version with database Version
    /// - If match: Update succeeds, Version is automatically incremented
    /// - If mismatch: Update fails with ConcurrencyException (409 Conflict)
    /// 
    /// CLIENT-SIDE REQUIREMENTS:
    /// - Client must include Version from GET response in PUT request
    /// - If update fails with 409 Conflict:
    ///   * Refresh waybill data (GET request)
    ///   * Show user that another user modified it
    ///   * Allow user to review changes and update again
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant can be updated. Cross-tenant updates are prevented.
    /// 
    /// </summary>
    /// <param name="waybillId">The waybill ID to update.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <param name="dto">The update request containing new field values and Version.</param>
    /// <returns>
    /// Updated WaybillDto if successful.
    /// Throws ConcurrencyException if version mismatch (concurrent update detected).
    /// Returns null if waybill not found or belongs to different tenant.
    /// </returns>
    Task<WaybillDto?> UpdateWaybillAsync(string waybillId, string tenantId, UpdateWaybillDto dto);

    /// <summary>
    /// Gets comprehensive summary statistics for a specific supplier.
    /// 
    /// This method calculates aggregated statistics about a supplier's waybill activity,
    /// including totals, averages, and status breakdown. All calculations are performed
    /// at the database level for optimal performance.
    /// 
    /// CALCULATIONS:
    /// - TotalDeliveries: Count of all waybills from this supplier
    /// - TotalQuantity: Sum of all quantities from this supplier
    /// - TotalAmount: Sum of all total amounts from this supplier
    /// - AverageQuantityPerDelivery: TotalQuantity / TotalDeliveries (0 if no deliveries)
    /// - StatusBreakdown: Count of waybills grouped by status
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that:
    /// 1. Only waybills belonging to the specified tenant are included
    /// 2. Only suppliers belonging to the specified tenant can be queried
    /// 
    /// This prevents cross-tenant data access even if a supplier ID from another
    /// tenant is provided.
    /// 
    /// </summary>
    /// <param name="supplierId">The supplier ID to get summary for.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>
    /// SupplierSummaryResponseDto containing all supplier statistics.
    /// Returns null if supplier not found or belongs to different tenant.
    /// </returns>
    Task<SupplierSummaryResponseDto?> GetSupplierSummaryAsync(string supplierId, string tenantId);

    /// <summary>
    /// Generates a monthly report for a specific year and month.
    /// 
    /// This method queries waybills for the specified month and calculates
    /// comprehensive statistics including totals, status breakdown, top suppliers,
    /// top projects, and product breakdown.
    /// 
    /// TENANT ISOLATION:
    /// All waybills are automatically filtered by tenant through the global query filter.
    /// 
    /// DATE FILTERING:
    /// Only waybills with waybill_date in the specified year and month are included.
    /// </summary>
    /// <param name="year">The year for the report (e.g., 2024).</param>
    /// <param name="month">The month for the report (1-12).</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>MonthlyReportResultDto containing comprehensive monthly statistics.</returns>
    Task<MonthlyReportResultDto> GenerateMonthlyReportAsync(int year, int month, string tenantId);
}
