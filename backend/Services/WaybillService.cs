using Microsoft.EntityFrameworkCore;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Exceptions;
using WaybillManagementSystem.Models.Enums;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for querying waybill data.
/// 
/// IMPLEMENTATION DETAILS:
/// This service provides efficient database queries for retrieving waybill data
/// with filtering, pagination, and search capabilities. All queries use IQueryable
/// to ensure optimal SQL generation and proper use of database indexes.
/// 
/// TENANT ISOLATION:
/// All queries automatically filter by tenant through the global query filter in
/// ApplicationDbContext. This ensures that:
/// 1. Only waybills belonging to the current tenant are returned
/// 2. Cross-tenant data access is impossible
/// 3. No explicit tenant filtering is needed in query code
/// 
/// However, we still verify the tenantId parameter matches the current context
/// as a security measure.
/// 
/// QUERY OPTIMIZATION:
/// - Uses IQueryable for deferred execution and SQL optimization
/// - Applies filters at database level (not in memory)
/// - Uses database indexes (TenantId, Status, WaybillDate, ProjectId, SupplierId)
/// - Includes related entities (Project, Supplier) in single query
/// - Implements efficient pagination using Skip/Take
/// 
/// HEBREW TEXT SEARCH:
/// Text search uses EF.Functions.Like with Unicode-aware pattern matching.
/// SQL Server's Unicode string comparison properly handles Hebrew characters,
/// allowing case-insensitive searches in Hebrew text.
/// </summary>
public class WaybillService : IWaybillService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<WaybillService> _logger;

    /// <summary>
    /// Initializes a new instance of the WaybillService.
    /// </summary>
    /// <param name="dbContext">Database context for querying waybill data.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="cacheService">Service for caching frequently accessed data.</param>
    /// <param name="logger">Logger for recording service operations.</param>
    public WaybillService(
        ApplicationDbContext dbContext,
        ITenantService tenantService,
        ICacheService cacheService,
        ILogger<WaybillService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger;
    }

    /// <summary>
    /// Gets a waybill by its ID.
    /// 
    /// This method retrieves a single waybill with all related information.
    /// The global query filter automatically ensures tenant isolation.
    /// 
    /// QUERY OPTIMIZATION:
    /// Uses Include() to eagerly load Project and Supplier in a single database query,
    /// avoiding N+1 query problems. The query is optimized by EF Core to use JOINs.
    /// </summary>
    /// <param name="id">The waybill ID to retrieve.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>WaybillDto if found, null otherwise.</returns>
    public async Task<WaybillDto?> GetByIdAsync(string id, string tenantId)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in GetByIdAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return null;
        }

        _logger.LogDebug("Retrieving waybill {WaybillId} for tenant {TenantId}", id, tenantId);

        // Query waybill with related entities
        // The global query filter automatically filters by TenantId
        // Include() eagerly loads Project and Supplier to avoid N+1 queries
        var waybill = await _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (waybill == null)
        {
            _logger.LogDebug("Waybill {WaybillId} not found for tenant {TenantId}", id, tenantId);
            return null;
        }

        // Map entity to DTO
        return MapToWaybillDto(waybill);
    }

    /// <summary>
    /// Gets a paginated list of waybills with optional filtering.
    /// 
    /// This method implements comprehensive filtering and pagination:
    /// - Date range filtering (waybill_date, delivery_date)
    /// - Status filtering
    /// - Project/Supplier/Product filtering
    /// - Hebrew text search
    /// - Pagination
    /// 
    /// QUERY OPTIMIZATION:
    /// - Filters are applied using IQueryable, ensuring SQL-level filtering
    /// - Uses database indexes for efficient filtering
    /// - Pagination uses Skip/Take for efficient data retrieval
    /// - Total count is calculated separately for performance
    /// 
    /// HEBREW TEXT SEARCH:
    /// The SearchText parameter uses EF.Functions.Like with Unicode-aware pattern
    /// matching. This properly handles Hebrew characters and allows case-insensitive
    /// searches in Hebrew text.
    /// 
    /// PERFORMANCE CONSIDERATIONS:
    /// - For large datasets, consider adding additional indexes
    /// - Text search may be slower on very large tables (consider full-text search)
    /// - Pagination ensures reasonable response sizes
    /// </summary>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <param name="query">Query parameters for filtering and pagination.</param>
    /// <returns>PagedResultDto containing waybills and pagination metadata.</returns>
    public async Task<PagedResultDto<WaybillListDto>> GetAllAsync(string tenantId, WaybillQueryDto query)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in GetAllAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return new PagedResultDto<WaybillListDto>
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = 0
            };
        }

        _logger.LogDebug(
            "Querying waybills for tenant {TenantId} with filters: Status={Status}, ProjectId={ProjectId}, SupplierId={SupplierId}, SearchText={SearchText}",
            tenantId,
            query.Status,
            query.ProjectId,
            query.SupplierId,
            query.SearchText);

        // ============================================================================
        // BUILD QUERY WITH FILTERS
        // ============================================================================
        // Start with base query - global query filter automatically applies TenantId filter
        // Include related entities to avoid N+1 queries
        var baseQuery = _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .AsQueryable();

        // Apply filters (all at database level for optimal performance)

        // Date range filter - waybill_date
        if (query.DateFrom.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.WaybillDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.WaybillDate <= query.DateTo.Value);
        }

        // Date range filter - delivery_date
        if (query.DeliveryDateFrom.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.DeliveryDate >= query.DeliveryDateFrom.Value);
        }

        if (query.DeliveryDateTo.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.DeliveryDate <= query.DeliveryDateTo.Value);
        }

        // Status filter
        if (query.Status.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.Status == query.Status.Value);
        }

        // Project filter
        if (!string.IsNullOrWhiteSpace(query.ProjectId))
        {
            baseQuery = baseQuery.Where(w => w.ProjectId == query.ProjectId);
        }

        // Supplier filter
        if (!string.IsNullOrWhiteSpace(query.SupplierId))
        {
            baseQuery = baseQuery.Where(w => w.SupplierId == query.SupplierId);
        }

        // Product code filter
        if (!string.IsNullOrWhiteSpace(query.ProductCode))
        {
            baseQuery = baseQuery.Where(w => w.ProductCode == query.ProductCode);
        }

        // Hebrew text search
        // WHY EF.Functions.Like: This uses SQL Server's LIKE operator with Unicode-aware
        // string comparison, which properly handles Hebrew characters. The pattern uses
        // wildcards (%) to allow partial matches.
        //
        // HEBREW TEXT SEARCH CONSIDERATIONS:
        // - Case-insensitive: SQL Server's default collation handles this
        // - Unicode-aware: Properly matches Hebrew Unicode characters
        // - Partial matching: %pattern% allows finding text anywhere in the field
        // - Performance: Uses database indexes when possible, but may be slower on large tables
        //
        // ALTERNATIVE: For better performance on very large datasets, consider:
        // - Full-text search indexes
        // - Search service (Elasticsearch, Azure Cognitive Search)
        // - Cached search results
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchPattern = $"%{query.SearchText.Trim()}%";
            baseQuery = baseQuery.Where(w =>
                EF.Functions.Like(w.Project.Name, searchPattern) ||
                EF.Functions.Like(w.Supplier.Name, searchPattern) ||
                EF.Functions.Like(w.ProductName, searchPattern));
        }

        // ============================================================================
        // PAGINATION
        // ============================================================================
        // Get total count before pagination (for pagination metadata)
        // This count query is optimized by EF Core and uses the same filters
        var totalCount = await baseQuery.CountAsync();

        // Apply pagination
        // Skip/Take are translated to SQL OFFSET/FETCH, which is efficient
        // The database only returns the requested page of data
        var waybills = await baseQuery
            .OrderByDescending(w => w.WaybillDate) // Order by most recent first
            .ThenByDescending(w => w.CreatedAt)   // Then by creation time
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} waybills (page {PageNumber} of {TotalPages}) for tenant {TenantId}",
            waybills.Count,
            query.PageNumber,
            (int)Math.Ceiling((double)totalCount / query.PageSize),
            tenantId);

        // Map entities to DTOs
        var items = waybills.Select(MapToWaybillListDto).ToList();

        return new PagedResultDto<WaybillListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// Gets all waybills for a specific project.
    /// 
    /// This method retrieves all waybills associated with a project. The global
    /// query filter ensures that only waybills for the current tenant are returned,
    /// and that the project belongs to the current tenant.
    /// 
    /// QUERY OPTIMIZATION:
    /// - Uses index on ProjectId for efficient filtering
    /// - Includes related entities in single query
    /// - Orders by date for consistent results
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>List of waybills for the specified project.</returns>
    public async Task<List<WaybillListDto>> GetByProjectIdAsync(string projectId, string tenantId)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in GetByProjectIdAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return new List<WaybillListDto>();
        }

        _logger.LogDebug(
            "Retrieving waybills for project {ProjectId} and tenant {TenantId}",
            projectId,
            tenantId);

        // Query waybills for the project
        // The global query filter automatically ensures:
        // 1. Only waybills for the current tenant are returned
        // 2. Only projects for the current tenant can be queried
        var waybills = await _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .Where(w => w.ProjectId == projectId)
            .OrderByDescending(w => w.WaybillDate)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} waybills for project {ProjectId} and tenant {TenantId}",
            waybills.Count,
            projectId,
            tenantId);

        // Map entities to DTOs
        return waybills.Select(MapToWaybillListDto).ToList();
    }

    /// <summary>
    /// Maps a Waybill entity to WaybillDto.
    /// </summary>
    /// <param name="waybill">The waybill entity to map.</param>
    /// <returns>A WaybillDto instance.</returns>
    private static WaybillDto MapToWaybillDto(Models.Waybill waybill)
    {
        return new WaybillDto
        {
            Id = waybill.Id,
            WaybillDate = waybill.WaybillDate,
            DeliveryDate = waybill.DeliveryDate,
            ProjectId = waybill.ProjectId,
            ProjectName = waybill.Project?.Name ?? string.Empty,
            SupplierId = waybill.SupplierId,
            SupplierName = waybill.Supplier?.Name ?? string.Empty,
            ProductCode = waybill.ProductCode,
            ProductName = waybill.ProductName,
            Quantity = waybill.Quantity,
            Unit = waybill.Unit,
            UnitPrice = waybill.UnitPrice,
            TotalAmount = waybill.TotalAmount,
            Currency = waybill.Currency,
            Status = waybill.Status,
            VehicleNumber = waybill.VehicleNumber,
            DriverName = waybill.DriverName,
            DeliveryAddress = waybill.DeliveryAddress,
            Notes = waybill.Notes,
            CreatedAt = waybill.CreatedAt,
            UpdatedAt = waybill.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a Waybill entity to WaybillListDto (simplified version).
    /// </summary>
    /// <param name="waybill">The waybill entity to map.</param>
    /// <returns>A WaybillListDto instance.</returns>
    private static WaybillListDto MapToWaybillListDto(Models.Waybill waybill)
    {
        return new WaybillListDto
        {
            Id = waybill.Id,
            WaybillDate = waybill.WaybillDate,
            DeliveryDate = waybill.DeliveryDate,
            ProjectName = waybill.Project?.Name ?? string.Empty,
            SupplierName = waybill.Supplier?.Name ?? string.Empty,
            ProductCode = waybill.ProductCode,
            ProductName = waybill.ProductName,
            TotalAmount = waybill.TotalAmount,
            Currency = waybill.Currency,
            Status = waybill.Status
        };
    }

    /// <summary>
    /// Gets comprehensive summary statistics for waybills.
    /// 
    /// This method performs multiple aggregations to provide business intelligence
    /// and analytics. All aggregations are performed at the database level using
    /// LINQ GroupBy operations, ensuring optimal performance.
    /// 
    /// AGGREGATION STRATEGY:
    /// 1. Base query: Filter by tenant (automatic via global query filter) and optional date range
    /// 2. Include related entities (Project, Supplier) to avoid N+1 queries
    /// 3. Execute aggregations in memory after loading data (for complex groupings)
    /// 
    /// PERFORMANCE CONSIDERATIONS:
    /// - Date range filtering reduces dataset size significantly
    /// - Database indexes on TenantId, Status, WaybillDate optimize filtering
    /// - GroupBy operations are executed efficiently by EF Core
    /// - For very large datasets, consider materialized views or caching
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant are included in all calculations.
    /// </summary>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <param name="dateFrom">Optional start date for filtering (inclusive).</param>
    /// <param name="dateTo">Optional end date for filtering (inclusive).</param>
    /// <returns>WaybillSummaryDto containing all aggregated statistics.</returns>
    public async Task<WaybillSummaryDto> GetSummaryAsync(string tenantId, DateTime? dateFrom, DateTime? dateTo)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in GetSummaryAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return new WaybillSummaryDto(); // Return empty summary
        }

        // Build cache key
        var dateRangeKey = dateFrom.HasValue && dateTo.HasValue
            ? $"{dateFrom.Value:yyyy-MM-dd}:{dateTo.Value:yyyy-MM-dd}"
            : "all";
        var cacheKey = $"waybill:summary:{tenantId}:{dateRangeKey}";

        // Try to get from cache
        var cachedSummary = await _cacheService.GetAsync<WaybillSummaryDto>(cacheKey);
        if (cachedSummary != null)
        {
            _logger.LogDebug("Retrieved waybill summary from cache for tenant {TenantId}", tenantId);
            return cachedSummary;
        }

        _logger.LogInformation(
            "Calculating waybill summary for tenant {TenantId} (dateFrom: {DateFrom}, dateTo: {DateTo})",
            tenantId,
            dateFrom,
            dateTo);

        // ============================================================================
        // BUILD BASE QUERY WITH FILTERS
        // ============================================================================
        // Start with base query - global query filter automatically applies TenantId filter
        // Include related entities to avoid N+1 queries
        var baseQuery = _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .AsQueryable();

        // Apply optional date range filter
        // WHY: Filtering at database level reduces data transfer and improves performance
        if (dateFrom.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.WaybillDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.WaybillDate <= dateTo.Value);
        }

        // Load all waybills into memory for complex aggregations
        // WHY: Some aggregations (like monthly breakdown) are easier to do in memory
        // For very large datasets, consider using raw SQL or materialized views
        var waybills = await baseQuery.ToListAsync();

        var totalWaybillCount = waybills.Count;

        // ============================================================================
        // CALCULATE TOTALS BY STATUS
        // ============================================================================
        // Group waybills by status and calculate totals
        // WHY: Provides breakdown of quantity and amount by status, useful for
        // understanding the distribution of waybills across different states
        var totalQuantityByStatus = waybills
            .GroupBy(w => w.Status)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(w => w.Quantity));

        var totalAmountByStatus = waybills
            .GroupBy(w => w.Status)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(w => w.TotalAmount));

        // ============================================================================
        // CALCULATE MONTHLY BREAKDOWN
        // ============================================================================
        // Group waybills by year and month, then calculate totals
        // WHY: Provides time-series view for trend analysis and period-over-period comparisons
        // The grouping is done by extracting Year and Month from WaybillDate
        var monthlyBreakdown = waybills
            .GroupBy(w => new { w.WaybillDate.Year, w.WaybillDate.Month })
            .Select(g => new MonthlySummaryDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount),
                DeliveryCount = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        // ============================================================================
        // CALCULATE TOP SUPPLIERS
        // ============================================================================
        // Group waybills by supplier, calculate totals, order by amount, take top 10
        // WHY: Identifies the most important suppliers by financial volume
        // This helps with supplier relationship management and procurement decisions
        var topSuppliers = waybills
            .GroupBy(w => new { w.SupplierId, SupplierName = w.Supplier != null ? w.Supplier.Name : string.Empty })
            .Select(g => new SupplierSummaryDto
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierName,
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount),
                DeliveryCount = g.Count()
            })
            .OrderByDescending(s => s.TotalAmount)
            .Take(10)
            .ToList();

        // ============================================================================
        // CALCULATE PROJECT TOTALS
        // ============================================================================
        // Group waybills by project, calculate totals, order by amount
        // WHY: Identifies the most active projects by financial volume
        // This helps with project management and resource allocation
        var projectTotals = waybills
            .GroupBy(w => new { w.ProjectId, ProjectName = w.Project != null ? w.Project.Name : string.Empty })
            .Select(g => new ProjectSummaryDto
            {
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.ProjectName,
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount),
                DeliveryCount = g.Count()
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // ============================================================================
        // CALCULATE QUALITY METRICS
        // ============================================================================
        // Count disputed and cancelled waybills, calculate percentages
        // WHY: Provides quality metrics to assess delivery performance
        // High percentages may indicate systemic issues that need attention
        var disputedCount = waybills.Count(w => w.Status == WaybillStatus.Disputed);
        var cancelledCount = waybills.Count(w => w.Status == WaybillStatus.Cancelled);

        var disputedPercentage = totalWaybillCount > 0
            ? (decimal)disputedCount / totalWaybillCount * 100
            : 0;

        var cancelledPercentage = totalWaybillCount > 0
            ? (decimal)cancelledCount / totalWaybillCount * 100
            : 0;

        _logger.LogInformation(
            "Summary calculated for tenant {TenantId}: {TotalCount} waybills, {DisputedCount} disputed ({DisputedPercentage:F2}%), {CancelledCount} cancelled ({CancelledPercentage:F2}%)",
            tenantId,
            totalWaybillCount,
            disputedCount,
            disputedPercentage,
            cancelledCount,
            cancelledPercentage);

        var summary = new WaybillSummaryDto
        {
            TotalQuantityByStatus = totalQuantityByStatus,
            TotalAmountByStatus = totalAmountByStatus,
            MonthlyBreakdown = monthlyBreakdown,
            TopSuppliers = topSuppliers,
            ProjectTotals = projectTotals,
            DisputedCount = disputedCount,
            CancelledCount = cancelledCount,
            DisputedPercentage = disputedPercentage,
            CancelledPercentage = cancelledPercentage
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, summary);

        return summary;
    }

    /// <summary>
    /// Updates the status of a waybill with business rule validation.
    /// 
    /// This method validates status transitions according to business rules before
    /// updating the waybill. It ensures data integrity and prevents invalid state changes.
    /// 
    /// STATUS TRANSITION VALIDATION:
    /// The method validates the transition from the current status to the new status
    /// according to the following rules:
    /// 
    /// ALLOWED TRANSITIONS:
    /// 1. PENDING → DELIVERED
    ///    - Meaning: Waybill was successfully delivered
    ///    - Use case: Mark waybill as completed after successful delivery
    /// 
    /// 2. PENDING → CANCELLED
    ///    - Meaning: Waybill was cancelled before delivery
    ///    - Use case: Cancel waybill due to order cancellation, supplier issues, etc.
    /// 
    /// 3. DELIVERED → DISPUTED
    ///    - Meaning: Delivery has issues or disputes
    ///    - Use case: Mark delivered waybill as disputed if there are quality issues,
    ///      quantity discrepancies, or other problems
    /// 
    /// NOT ALLOWED TRANSITIONS:
    /// 1. CANCELLED → anything
    ///    - Reason: Cancelled waybills are in a final state and cannot be changed
    ///    - Error message: "Cannot change status of a cancelled waybill. Cancelled waybills are final and cannot be modified."
    /// 
    /// 2. DISPUTED → anything
    ///    - Reason: Disputed waybills must be resolved through other means (e.g., manual review)
    ///    - Error message: "Cannot change status of a disputed waybill. Disputes must be resolved through manual review."
    /// 
    /// 3. DELIVERED → PENDING or CANCELLED
    ///    - Reason: Once delivered, a waybill cannot be moved back to pending or cancelled
    ///    - Error message: "Cannot change status from DELIVERED to {newStatus}. Once delivered, a waybill cannot be moved back to PENDING or CANCELLED."
    /// 
    /// 4. Any other transition not explicitly allowed
    ///    - Reason: Only specific transitions are allowed for data integrity
    ///    - Error message: "Invalid status transition from {currentStatus} to {newStatus}. Allowed transitions: [list of allowed transitions]"
    /// 
    /// VALIDATION LOGIC:
    /// 1. Load waybill from database (with tenant filter)
    /// 2. Check if waybill exists and belongs to tenant
    /// 3. Validate status transition (current status → new status)
    /// 4. If valid, update status and notes
    /// 5. Update UpdatedAt timestamp
    /// 6. Save changes to database
    /// 
    /// NOTES FIELD:
    /// If notes are provided, they are appended to the existing notes (if any) with
    /// a timestamp prefix. This creates an audit trail of status changes.
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that only waybills belonging
    /// to the specified tenant can be updated. If a waybill ID exists for another
    /// tenant, it will not be found and the method returns null.
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
    public async Task<WaybillDto?> UpdateStatusAsync(string waybillId, string tenantId, UpdateWaybillStatusDto dto)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in UpdateStatusAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return null;
        }

        _logger.LogInformation(
            "Updating status of waybill {WaybillId} for tenant {TenantId} to {NewStatus}",
            waybillId,
            tenantId,
            dto.Status);

        // Load waybill from database
        // The global query filter automatically ensures tenant isolation
        var waybill = await _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .FirstOrDefaultAsync(w => w.Id == waybillId);

        if (waybill == null)
        {
            _logger.LogWarning(
                "Waybill {WaybillId} not found for tenant {TenantId}",
                waybillId,
                tenantId);
            return null;
        }

        // ============================================================================
        // VALIDATE STATUS TRANSITION
        // ============================================================================
        var currentStatus = waybill.Status;
        var newStatus = dto.Status;

        // If status is already the same, no need to update (but still allow notes update)
        if (currentStatus == newStatus)
        {
            _logger.LogDebug(
                "Waybill {WaybillId} already has status {Status}, updating notes only",
                waybillId,
                currentStatus);
        }
        else
        {
            // Validate status transition according to business rules
            var isValidTransition = IsValidStatusTransition(currentStatus, newStatus);

            if (!isValidTransition)
            {
                var errorMessage = GetStatusTransitionErrorMessage(currentStatus, newStatus);
                _logger.LogWarning(
                    "Invalid status transition for waybill {WaybillId}: {CurrentStatus} → {NewStatus}. {ErrorMessage}",
                    waybillId,
                    currentStatus,
                    newStatus,
                    errorMessage);

                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation(
                "Status transition validated for waybill {WaybillId}: {CurrentStatus} → {NewStatus}",
                waybillId,
                currentStatus,
                newStatus);
        }

        // ============================================================================
        // UPDATE WAYBILL
        // ============================================================================
        // Update status if it changed
        if (currentStatus != newStatus)
        {
            waybill.Status = newStatus;
        }

        // Update notes if provided
        // Append to existing notes with timestamp to create audit trail
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var statusChangeNote = $"[{timestamp}] Status changed to {newStatus}: {dto.Notes.Trim()}";

            if (string.IsNullOrWhiteSpace(waybill.Notes))
            {
                waybill.Notes = statusChangeNote;
            }
            else
            {
                waybill.Notes = $"{waybill.Notes}\n{statusChangeNote}";
            }
        }

        // Update timestamp
        waybill.UpdatedAt = DateTime.UtcNow;

        // Save changes to database
        await _dbContext.SaveChangesAsync();

        // Invalidate cache after status update
        await _cacheService.RemoveByPatternAsync($"waybill:summary:{tenantId}:*");
        await _cacheService.RemoveByPatternAsync($"supplier:summary:{tenantId}:*");

        _logger.LogInformation(
            "Successfully updated status of waybill {WaybillId} to {NewStatus} for tenant {TenantId}",
            waybillId,
            newStatus,
            tenantId);

        // Return updated waybill
        return MapToWaybillDto(waybill);
    }

    /// <summary>
    /// Validates if a status transition is allowed according to business rules.
    /// 
    /// STATUS TRANSITION RULES:
    /// - PENDING → DELIVERED: Allowed (successful delivery)
    /// - PENDING → CANCELLED: Allowed (cancelled before delivery)
    /// - DELIVERED → DISPUTED: Allowed (delivery has issues)
    /// - CANCELLED → anything: NOT allowed (final state)
    /// - DISPUTED → anything: NOT allowed (must be resolved manually)
    /// - DELIVERED → PENDING or CANCELLED: NOT allowed (cannot go backwards)
    /// - Any other transition: NOT allowed
    /// 
    /// </summary>
    /// <param name="currentStatus">The current status of the waybill.</param>
    /// <param name="newStatus">The desired new status.</param>
    /// <returns>True if transition is allowed, false otherwise.</returns>
    private static bool IsValidStatusTransition(WaybillStatus currentStatus, WaybillStatus newStatus)
    {
        // Same status is always valid (allows notes update)
        if (currentStatus == newStatus)
        {
            return true;
        }

        // CANCELLED is a final state - cannot transition from it
        if (currentStatus == WaybillStatus.Cancelled)
        {
            return false;
        }

        // DISPUTED is a final state - cannot transition from it
        if (currentStatus == WaybillStatus.Disputed)
        {
            return false;
        }

        // DELIVERED can only transition to DISPUTED
        if (currentStatus == WaybillStatus.Delivered)
        {
            return newStatus == WaybillStatus.Disputed;
        }

        // PENDING can transition to DELIVERED or CANCELLED
        if (currentStatus == WaybillStatus.Pending)
        {
            return newStatus == WaybillStatus.Delivered || newStatus == WaybillStatus.Cancelled;
        }

        // Any other transition is not allowed
        return false;
    }

    /// <summary>
    /// Gets a descriptive error message explaining why a status transition was rejected.
    /// 
    /// This method provides clear, user-friendly error messages that explain:
    /// - Why the transition is not allowed
    /// - What the current status means
    /// - What transitions are allowed from the current status
    /// 
    /// </summary>
    /// <param name="currentStatus">The current status of the waybill.</param>
    /// <param name="newStatus">The desired new status.</param>
    /// <returns>A descriptive error message.</returns>
    private static string GetStatusTransitionErrorMessage(WaybillStatus currentStatus, WaybillStatus newStatus)
    {
        // Same status should not reach here, but handle it anyway
        if (currentStatus == newStatus)
        {
            return "Status is already set to the requested value.";
        }

        // CANCELLED is a final state
        if (currentStatus == WaybillStatus.Cancelled)
        {
            return "Cannot change status of a cancelled waybill. Cancelled waybills are final and cannot be modified. " +
                   "If you need to reactivate this waybill, please create a new one.";
        }

        // DISPUTED is a final state
        if (currentStatus == WaybillStatus.Disputed)
        {
            return "Cannot change status of a disputed waybill. Disputes must be resolved through manual review. " +
                   "Please contact support to resolve the dispute.";
        }

        // DELIVERED can only go to DISPUTED
        if (currentStatus == WaybillStatus.Delivered)
        {
            if (newStatus == WaybillStatus.Pending || newStatus == WaybillStatus.Cancelled)
            {
                return $"Cannot change status from DELIVERED to {newStatus}. Once delivered, a waybill cannot be moved back to PENDING or CANCELLED. " +
                       "If there are issues with the delivery, change the status to DISPUTED instead.";
            }
            else
            {
                return $"Invalid status transition from DELIVERED to {newStatus}. " +
                       "From DELIVERED status, only transition to DISPUTED is allowed (if there are issues with the delivery).";
            }
        }

        // PENDING can go to DELIVERED or CANCELLED
        if (currentStatus == WaybillStatus.Pending)
        {
            var allowedTransitions = "DELIVERED (if delivery was successful) or CANCELLED (if the waybill was cancelled)";
            return $"Invalid status transition from PENDING to {newStatus}. " +
                   $"From PENDING status, only transitions to {allowedTransitions} are allowed.";
        }

        // Generic error for any other case
        return $"Invalid status transition from {currentStatus} to {newStatus}. " +
               "Please check the business rules for allowed status transitions.";
    }

    /// <summary>
    /// Updates a waybill with optimistic locking to prevent concurrent update conflicts.
    /// 
    /// This method implements optimistic locking by checking the Version property before
    /// applying updates. If the version doesn't match, it means another user has modified
    /// the waybill, and a ConcurrencyException is thrown.
    /// 
    /// OPTIMISTIC LOCKING MECHANISM:
    /// Optimistic locking is a concurrency control strategy that assumes conflicts are rare.
    /// Instead of locking the database row (pessimistic locking), it checks if the entity
    /// has been modified by comparing version numbers.
    /// 
    /// HOW IT WORKS:
    /// 1. Client loads waybill (GET /api/waybills/{id}) and receives Version value
    /// 2. Client modifies waybill data and sends PUT request with Version
    /// 3. Server loads waybill from database
    /// 4. Server compares client's Version with database Version:
    ///    - If versions match: Entity hasn't been modified, safe to update
    ///    - If versions don't match: Entity was modified by another user, throw ConcurrencyException
    /// 5. If versions match:
    ///    - Update all fields from DTO
    ///    - Update UpdatedAt timestamp
    ///    - Save changes (EF Core automatically increments Version)
    /// 6. If versions don't match:
    ///    - Throw ConcurrencyException
    ///    - Client receives 409 Conflict response
    ///    - Client should refresh data and try again
    /// 
    /// VERSION PROPERTY:
    /// The Version property is a byte array (rowversion in SQL Server) that is:
    /// - Automatically set by the database on insert
    /// - Automatically incremented by the database on each update
    /// - Configured with [Timestamp] attribute in the entity
    /// - Configured with IsRowVersion() in DbContext
    /// 
    /// EF CORE CONCURRENCY HANDLING:
    /// EF Core automatically handles version checking when:
    /// - Property is marked with [Timestamp] attribute
    /// - Property is configured with IsRowVersion() in DbContext
    /// - DbUpdateConcurrencyException is thrown if version mismatch
    /// 
    /// We catch DbUpdateConcurrencyException and convert it to ConcurrencyException
    /// for better error handling and clearer error messages.
    /// 
    /// CLIENT-SIDE IMPLEMENTATION:
    /// When implementing the client:
    /// 1. Store Version from GET response
    /// 2. Include Version in PUT request body
    /// 3. Handle 409 Conflict response:
    ///    - Show user-friendly message
    ///    - Refresh waybill data (GET request)
    ///    - Display current values
    ///    - Allow user to review changes and update again
    /// 
    /// CONCURRENT UPDATE SCENARIO:
    /// User A and User B both load waybill (Version = 0x01)
    /// User A modifies and saves (Version becomes 0x02)
    /// User B tries to save (Version mismatch: 0x01 != 0x02)
    /// User B receives 409 Conflict
    /// User B refreshes data (gets Version 0x02)
    /// User B can now update with correct version
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
    public async Task<WaybillDto?> UpdateWaybillAsync(string waybillId, string tenantId, UpdateWaybillDto dto)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in UpdateWaybillAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return null;
        }

        _logger.LogInformation(
            "Updating waybill {WaybillId} for tenant {TenantId}",
            waybillId,
            tenantId);

        // Load waybill from database
        // The global query filter automatically ensures tenant isolation
        var waybill = await _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .FirstOrDefaultAsync(w => w.Id == waybillId);

        if (waybill == null)
        {
            _logger.LogWarning(
                "Waybill {WaybillId} not found for tenant {TenantId}",
                waybillId,
                tenantId);
            return null;
        }

        // ============================================================================
        // OPTIMISTIC LOCKING: VERSION CHECK
        // ============================================================================
        // Compare client's version with database version
        // WHY: This detects if another user has modified the waybill since the client
        // last loaded it. If versions don't match, it means a concurrent update occurred.
        //
        // VERSION COMPARISON:
        // The Version property is a byte array (rowversion). We need to compare the
        // byte arrays to check if they match. If they don't match, another user has
        // modified the waybill.
        //
        // EF CORE AUTOMATIC CHECKING:
        // EF Core will also check the version when SaveChangesAsync() is called if the
        // property is configured with [Timestamp] and IsRowVersion(). However, we do
        // an explicit check here to provide a clearer error message before attempting
        // the save operation.
        if (!AreVersionsEqual(waybill.Version, dto.Version))
        {
            _logger.LogWarning(
                "Concurrency conflict detected for waybill {WaybillId}. " +
                "Client version: {ClientVersion}, Database version: {DatabaseVersion}",
                waybillId,
                Convert.ToBase64String(dto.Version),
                waybill.Version != null ? Convert.ToBase64String(waybill.Version) : "null");

            throw new ConcurrencyException(
                waybillId,
                "Waybill",
                $"Waybill with ID '{waybillId}' was modified by another user. Please refresh and try again.");
        }

        // ============================================================================
        // UPDATE WAYBILL FIELDS
        // ============================================================================
        // Versions match - safe to update
        // Update all fields from DTO
        waybill.WaybillDate = dto.WaybillDate;
        waybill.DeliveryDate = dto.DeliveryDate;
        waybill.ProjectId = dto.ProjectId;
        waybill.SupplierId = dto.SupplierId;
        waybill.ProductCode = dto.ProductCode;
        waybill.ProductName = dto.ProductName;
        waybill.Quantity = dto.Quantity;
        waybill.Unit = dto.Unit;
        waybill.UnitPrice = dto.UnitPrice;
        waybill.TotalAmount = dto.TotalAmount;
        waybill.Currency = dto.Currency;
        waybill.Status = dto.Status;
        waybill.VehicleNumber = dto.VehicleNumber;
        waybill.DriverName = dto.DriverName;
        waybill.DeliveryAddress = dto.DeliveryAddress;
        waybill.Notes = dto.Notes;
        waybill.UpdatedAt = DateTime.UtcNow;

        // Note: We don't update Version here - EF Core will automatically update it
        // when SaveChangesAsync() is called because it's configured with [Timestamp]

        try
        {
            // Save changes to database
            // EF Core will:
            // 1. Check version again (defense in depth)
            // 2. If version still matches: Update entity and increment Version
            // 3. If version changed: Throw DbUpdateConcurrencyException
            await _dbContext.SaveChangesAsync();

            // Invalidate cache after waybill update
            await _cacheService.RemoveByPatternAsync($"waybill:summary:{tenantId}:*");
            await _cacheService.RemoveByPatternAsync($"supplier:summary:{tenantId}:*");

            _logger.LogInformation(
                "Successfully updated waybill {WaybillId} for tenant {TenantId}",
                waybillId,
                tenantId);

            // Reload waybill to get updated Version and related entities
            await _dbContext.Entry(waybill).ReloadAsync();

            // Return updated waybill
            return MapToWaybillDto(waybill);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // This exception is thrown by EF Core if version mismatch is detected
            // during SaveChangesAsync(). This is a defense-in-depth check.
            _logger.LogWarning(
                ex,
                "Concurrency conflict detected by EF Core for waybill {WaybillId}",
                waybillId);

            // Reload waybill to get current version
            await _dbContext.Entry(waybill).ReloadAsync();

            throw new ConcurrencyException(
                waybillId,
                "Waybill",
                $"Waybill with ID '{waybillId}' was modified by another user. Please refresh and try again.");
        }
    }

    /// <summary>
    /// Compares two version byte arrays to check if they are equal.
    /// 
    /// This method safely compares version arrays, handling null values.
    /// 
    /// </summary>
    /// <param name="version1">First version array (from database).</param>
    /// <param name="version2">Second version array (from client).</param>
    /// <returns>True if versions are equal, false otherwise.</returns>
    private static bool AreVersionsEqual(byte[]? version1, byte[]? version2)
    {
        // Both null - consider equal (shouldn't happen, but handle gracefully)
        if (version1 == null && version2 == null)
        {
            return true;
        }

        // One is null, other is not - not equal
        if (version1 == null || version2 == null)
        {
            return false;
        }

        // Compare byte arrays
        if (version1.Length != version2.Length)
        {
            return false;
        }

        for (int i = 0; i < version1.Length; i++)
        {
            if (version1[i] != version2[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets comprehensive summary statistics for a specific supplier.
    /// 
    /// This method calculates aggregated statistics about a supplier's waybill activity
    /// by querying all waybills from the supplier and performing aggregations. All
    /// calculations are performed at the database level using LINQ GroupBy operations
    /// for optimal performance.
    /// 
    /// CALCULATIONS:
    /// The method performs the following calculations:
    /// 1. TotalDeliveries: Count of all waybills from this supplier
    /// 2. TotalQuantity: Sum of all Quantity values
    /// 3. TotalAmount: Sum of all TotalAmount values
    /// 4. AverageQuantityPerDelivery: TotalQuantity / TotalDeliveries (handles division by zero)
    /// 5. StatusBreakdown: Count of waybills grouped by Status
    /// 
    /// QUERY OPTIMIZATION:
    /// - Uses IQueryable for efficient SQL generation
    /// - Aggregations performed at database level (not in memory)
    /// - Uses database indexes (TenantId, SupplierId) for fast filtering
    /// - Includes Supplier entity to get supplier name
    /// 
    /// TENANT ISOLATION:
    /// The global query filter automatically ensures that:
    /// 1. Only waybills belonging to the specified tenant are included
    /// 2. Only suppliers belonging to the specified tenant can be queried
    /// 
    /// If a supplier ID from another tenant is provided, the query will return
    /// no results (due to global query filter), and the method returns null.
    /// 
    /// </summary>
    /// <param name="supplierId">The supplier ID to get summary for.</param>
    /// <param name="tenantId">The tenant ID (verified against current context).</param>
    /// <returns>
    /// SupplierSummaryResponseDto containing all supplier statistics.
    /// Returns null if supplier not found or belongs to different tenant.
    /// </returns>
    public async Task<SupplierSummaryResponseDto?> GetSupplierSummaryAsync(string supplierId, string tenantId)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in GetSupplierSummaryAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            return null;
        }

        // Build cache key
        var cacheKey = $"supplier:summary:{tenantId}:{supplierId}";

        // Try to get from cache
        var cachedSummary = await _cacheService.GetAsync<SupplierSummaryResponseDto>(cacheKey);
        if (cachedSummary != null)
        {
            _logger.LogDebug("Retrieved supplier summary from cache for supplier {SupplierId} and tenant {TenantId}", supplierId, tenantId);
            return cachedSummary;
        }

        _logger.LogDebug(
            "Calculating supplier summary for supplier {SupplierId} and tenant {TenantId}",
            supplierId,
            tenantId);

        // ============================================================================
        // VERIFY SUPPLIER EXISTS AND BELONGS TO TENANT
        // ============================================================================
        // First, verify the supplier exists and belongs to the tenant
        // The global query filter ensures only suppliers for the current tenant are returned
        var supplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId);

        if (supplier == null)
        {
            _logger.LogWarning(
                "Supplier {SupplierId} not found for tenant {TenantId}",
                supplierId,
                tenantId);
            return null;
        }

        // ============================================================================
        // QUERY WAYBILLS FOR SUPPLIER
        // ============================================================================
        // Query all waybills from this supplier
        // The global query filter automatically ensures tenant isolation
        var waybills = await _dbContext.Waybills
            .Where(w => w.SupplierId == supplierId)
            .ToListAsync();

        // ============================================================================
        // CALCULATE STATISTICS
        // ============================================================================
        // Perform aggregations to calculate summary statistics
        // These calculations are done in memory after loading waybills, but for
        // large datasets, you could use database-level aggregations with GroupBy

        var totalDeliveries = waybills.Count;
        var totalQuantity = waybills.Sum(w => w.Quantity);
        var totalAmount = waybills.Sum(w => w.TotalAmount);

        // Calculate average quantity per delivery
        // Handle division by zero - if no deliveries, average is 0
        var averageQuantityPerDelivery = totalDeliveries > 0
            ? totalQuantity / totalDeliveries
            : 0;

        // Calculate status breakdown
        // Group waybills by status and count each group
        var statusBreakdown = waybills
            .GroupBy(w => w.Status)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        // Ensure all statuses are represented (even if count is 0)
        // This provides a complete picture of the supplier's status distribution
        var allStatuses = Enum.GetValues<WaybillStatus>();
        foreach (var status in allStatuses)
        {
            if (!statusBreakdown.ContainsKey(status))
            {
                statusBreakdown[status] = 0;
            }
        }

        _logger.LogInformation(
            "Supplier summary calculated for supplier {SupplierId} and tenant {TenantId}: " +
            "TotalDeliveries={TotalDeliveries}, TotalQuantity={TotalQuantity}, TotalAmount={TotalAmount}",
            supplierId,
            tenantId,
            totalDeliveries,
            totalQuantity,
            totalAmount);

        var summary = new SupplierSummaryResponseDto
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            TotalDeliveries = totalDeliveries,
            TotalQuantity = totalQuantity,
            TotalAmount = totalAmount,
            AverageQuantityPerDelivery = averageQuantityPerDelivery,
            StatusBreakdown = statusBreakdown
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, summary);

        return summary;
    }

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
    public async Task<MonthlyReportResultDto> GenerateMonthlyReportAsync(int year, int month, string tenantId)
    {
        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            _logger.LogWarning(
                "Tenant ID mismatch in GenerateMonthlyReportAsync. Expected {ExpectedTenantId}, got {ActualTenantId}",
                tenantId,
                currentTenantId);
            throw new InvalidOperationException($"Tenant ID mismatch. Expected {tenantId}, got {currentTenantId}");
        }

        _logger.LogInformation(
            "Generating monthly report for year {Year}, month {Month}, tenant {TenantId}",
            year,
            month,
            tenantId);

        // Validate month range
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");
        }

        // Calculate date range for the month
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Query waybills for the specified month
        // The global query filter automatically filters by tenant
        var waybills = await _dbContext.Waybills
            .Include(w => w.Project)
            .Include(w => w.Supplier)
            .Where(w => w.WaybillDate >= startDate && w.WaybillDate <= endDate)
            .ToListAsync();

        var totalWaybills = waybills.Count;
        var totalQuantity = waybills.Sum(w => w.Quantity);
        var totalAmount = waybills.Sum(w => w.TotalAmount);

        // Status breakdown
        var statusBreakdown = waybills
            .GroupBy(w => w.Status)
            .Select(g => new StatusBreakdownDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount)
            })
            .ToList();

        // Top suppliers (ordered by total amount, take top 10)
        var topSuppliers = waybills
            .GroupBy(w => new { w.SupplierId, SupplierName = w.Supplier != null ? w.Supplier.Name : string.Empty })
            .Select(g => new SupplierReportDto
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierName,
                DeliveryCount = g.Count(),
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount)
            })
            .OrderByDescending(s => s.TotalAmount)
            .Take(10)
            .ToList();

        // Top projects (ordered by total amount, take top 10)
        var topProjects = waybills
            .GroupBy(w => new { w.ProjectId, ProjectName = w.Project != null ? w.Project.Name : string.Empty })
            .Select(g => new ProjectReportDto
            {
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.ProjectName,
                WaybillCount = g.Count(),
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount)
            })
            .OrderByDescending(p => p.TotalAmount)
            .Take(10)
            .ToList();

        // Product breakdown
        var productBreakdown = waybills
            .GroupBy(w => new { w.ProductCode, w.ProductName })
            .Select(g => new ProductReportDto
            {
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                Count = g.Count(),
                TotalQuantity = g.Sum(w => w.Quantity),
                TotalAmount = g.Sum(w => w.TotalAmount)
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        _logger.LogInformation(
            "Monthly report generated for year {Year}, month {Month}, tenant {TenantId}: " +
            "TotalWaybills={TotalWaybills}, TotalQuantity={TotalQuantity}, TotalAmount={TotalAmount}",
            year,
            month,
            tenantId,
            totalWaybills,
            totalQuantity,
            totalAmount);

        return new MonthlyReportResultDto
        {
            Year = year,
            Month = month,
            GeneratedAt = DateTime.UtcNow,
            TotalWaybills = totalWaybills,
            TotalQuantity = totalQuantity,
            TotalAmount = totalAmount,
            StatusBreakdown = statusBreakdown,
            TopSuppliers = topSuppliers,
            TopProjects = topProjects,
            ProductBreakdown = productBreakdown
        };
    }
}
