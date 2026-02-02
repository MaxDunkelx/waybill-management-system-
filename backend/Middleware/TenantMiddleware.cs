using System.Net;

namespace WaybillManagementSystem.Middleware;

/// <summary>
/// Middleware for multi-tenant isolation in the Waybill Management System.
/// 
/// PURPOSE:
/// This middleware extracts the tenant ID from the HTTP request and makes it available
/// to the rest of the application pipeline. It ensures that every request is associated
/// with a tenant, enabling complete data isolation at the database level through
/// EF Core's global query filters.
/// 
/// HOW IT WORKS:
/// 1. Extracts tenant ID from the "X-Tenant-ID" HTTP header
/// 2. Validates that the tenant ID is present and not empty
/// 3. Optionally validates that the tenant exists in the database
/// 4. Stores the tenant ID in HttpContext.Items for use by services and DbContext
/// 5. Returns 400 Bad Request if tenant ID is missing or invalid
/// 
/// INTEGRATION WITH EF CORE GLOBAL QUERY FILTERS:
/// The ApplicationDbContext uses global query filters that automatically filter all
/// queries by TenantId. However, these filters need to know which tenant ID to use.
/// 
/// Flow:
/// 1. This middleware extracts tenant ID from request → stores in HttpContext.Items
/// 2. TenantService reads from HttpContext.Items → provides to services
/// 3. ApplicationDbContext uses TenantService → gets tenant ID for query filters
/// 4. All database queries automatically filtered → only return current tenant's data
/// 
/// This creates a complete isolation layer:
/// - Request level: Middleware validates tenant ID
/// - Service level: TenantService provides tenant context
/// - Database level: Global query filters enforce isolation
/// 
/// SECURITY CONSIDERATIONS:
/// - Tenant ID must be provided in every request (enforced by middleware)
/// - Invalid tenant IDs are rejected before reaching controllers
/// - Database queries are automatically filtered, preventing cross-tenant data access
/// - Even if application code forgets to filter by tenant, the global query filter ensures isolation
/// 
/// ALTERNATIVE APPROACHES:
/// Instead of HTTP headers, tenant ID could be extracted from:
/// - Subdomain (e.g., tenant1.yourapp.com)
/// - JWT token claims (for authenticated requests)
/// - URL path (e.g., /api/tenant/{tenantId}/waybills)
/// - Database lookup based on authenticated user
/// 
/// The header-based approach is simple and works well for API clients that can
/// set custom headers. For web applications, subdomain or JWT-based approaches
/// may be more appropriate.
/// </summary>
public class TenantMiddleware
{
    /// <summary>
    /// HTTP header name for tenant ID.
    /// Clients must include this header in every request with a valid tenant ID.
    /// </summary>
    public const string TenantIdHeaderName = "X-Tenant-ID";

    /// <summary>
    /// Key used to store tenant ID in HttpContext.Items dictionary.
    /// Services can access the tenant ID using this key.
    /// </summary>
    public const string TenantIdContextKey = "CurrentTenantId";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the TenantMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for recording middleware operations.</param>
    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// 
    /// PROCESSING FLOW:
    /// 1. Extract tenant ID from X-Tenant-ID header
    /// 2. Validate tenant ID is present and not empty
    /// 3. Store tenant ID in HttpContext.Items for downstream use
    /// 4. Continue to next middleware if valid, or return 400 if invalid
    /// 
    /// EXCEPTIONS:
    /// - Health check endpoints may bypass tenant validation (optional)
    /// - Swagger UI endpoints may bypass tenant validation (optional)
    /// 
    /// For production, you may want to add these exceptions or use a different
    /// approach for public endpoints that don't require tenant context.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant validation for certain endpoints that don't require tenant context
        // This is useful for health checks, Swagger UI, or other public endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // Allow CORS preflight requests (OPTIONS method) - these don't have tenant headers
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }
        
        // Allow Swagger UI and related endpoints (Swagger is configured as root with RoutePrefix = "")
        // Also allow explicit /swagger paths and static assets
        if (path.StartsWith("/swagger") || 
            path.StartsWith("/health") || 
            path == "/" ||
            path.StartsWith("/api/health") ||
            path.StartsWith("/index.html") ||
            path.EndsWith(".css") ||
            path.EndsWith(".js") ||
            path.EndsWith(".json") ||
            path.Contains("/swagger/"))
        {
            await _next(context);
            return;
        }

        // Extract tenant ID from HTTP header
        // The X-Tenant-ID header is a standard convention for multi-tenant APIs
        // SECURITY: The tenant ID can come from:
        // 1. X-Tenant-ID header (set via "Authorize" button in Swagger or manually)
        // 2. The header is required for all API requests to ensure tenant isolation
        if (!context.Request.Headers.TryGetValue(TenantIdHeaderName, out var tenantIdHeader) ||
            string.IsNullOrWhiteSpace(tenantIdHeader))
        {
            _logger.LogWarning(
                "Request to {Path} rejected: Missing or empty X-Tenant-ID header. " +
                "IP: {RemoteIpAddress}. " +
                "TIP: Use the 'Authorize' button in Swagger UI to set the tenant ID automatically.",
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            // Return 400 Bad Request if tenant ID is missing
            // This ensures that no request can proceed without a tenant context
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing tenant identifier",
                message = $"The '{TenantIdHeaderName}' header is required for all requests. " +
                         "Please use the 'Authorize' button in Swagger UI to set your tenant ID, " +
                         "or include the X-Tenant-ID header manually.",
                headerName = TenantIdHeaderName
            });

            return;
        }

        var tenantId = tenantIdHeader.ToString().Trim();

        // Additional validation: ensure tenant ID is not empty after trimming
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning(
                "Request to {Path} rejected: Empty tenant ID after trimming. " +
                "IP: {RemoteIpAddress}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid tenant identifier",
                message = "The tenant ID cannot be empty or whitespace.",
                headerName = TenantIdHeaderName
            });

            return;
        }

        // Store tenant ID in HttpContext.Items for use by services and DbContext
        // This dictionary is scoped to the current HTTP request and is available
        // to all services and middleware in the pipeline
        context.Items[TenantIdContextKey] = tenantId;

        _logger.LogDebug(
            "Tenant ID {TenantId} extracted for request to {Path}",
            tenantId,
            context.Request.Path);

        // Continue to the next middleware in the pipeline
        // The tenant ID is now available in HttpContext.Items for downstream components
        await _next(context);
    }
}
