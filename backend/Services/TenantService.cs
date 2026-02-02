using System.Net;
using WaybillManagementSystem.Middleware;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for accessing the current tenant context.
/// 
/// IMPLEMENTATION DETAILS:
/// This service reads the tenant ID from HttpContext.Items, which is populated
/// by the TenantMiddleware. The tenant ID is stored using the key defined in
/// TenantMiddleware.TenantIdContextKey.
/// 
/// LIFETIME:
/// Registered as Scoped, meaning one instance per HTTP request. This ensures that
/// each request has its own tenant context and there's no cross-request contamination.
/// 
/// INTEGRATION WITH EF CORE:
/// The ApplicationDbContext injects this service and uses GetCurrentTenantId() in
/// its global query filters. This creates a seamless integration where:
/// 1. Middleware extracts tenant ID → stores in HttpContext.Items
/// 2. TenantService reads from HttpContext.Items → provides to DbContext
/// 3. DbContext uses tenant ID in query filters → automatically filters all queries
/// 
/// This ensures that even if application code forgets to filter by tenant, the
/// database queries are automatically scoped to the current tenant.
/// </summary>
public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantService> _logger;

    /// <summary>
    /// Initializes a new instance of the TenantService.
    /// </summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <param name="logger">Logger for recording service operations.</param>
    public TenantService(IHttpContextAccessor httpContextAccessor, ILogger<TenantService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger;
    }

    /// <summary>
    /// Gets the current tenant ID from the HTTP request context.
    /// 
    /// This method reads the tenant ID that was stored by TenantMiddleware in
    /// HttpContext.Items. If the tenant ID is not available, it throws an
    /// InvalidOperationException, which indicates a configuration problem.
    /// 
    /// VALIDATION:
    /// The method validates that:
    /// 1. HttpContext is available (not null)
    /// 2. Tenant ID exists in HttpContext.Items
    /// 3. Tenant ID is not null or empty
    /// 
    /// ERROR HANDLING:
    /// If tenant ID is missing, this typically means:
    /// - TenantMiddleware is not registered in the pipeline
    /// - Request bypassed the middleware (e.g., health check endpoint)
    /// - Middleware failed to extract tenant ID
    /// 
    /// In production, you may want to handle these cases more gracefully,
    /// but throwing an exception ensures that tenant context is always available
    /// when expected.
    /// </summary>
    /// <returns>The current tenant ID.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if tenant ID is not available in the current context.
    /// </exception>
    public string GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null. TenantService cannot access tenant ID.");
            throw new InvalidOperationException(
                "HttpContext is not available. TenantService can only be used within an HTTP request context.");
        }

        // Retrieve tenant ID from HttpContext.Items (set by TenantMiddleware)
        if (!httpContext.Items.TryGetValue(TenantMiddleware.TenantIdContextKey, out var tenantIdObj) ||
            tenantIdObj == null)
        {
            var path = httpContext.Request.Path.Value ?? "unknown";
            _logger.LogError(
                "Tenant ID not found in HttpContext.Items for request to {Path}. " +
                "Ensure TenantMiddleware is registered in the pipeline.",
                path);

            throw new InvalidOperationException(
                $"Tenant ID is not available in the current context. " +
                $"This typically means TenantMiddleware is not configured or the request bypassed it. " +
                $"Request path: {path}");
        }

        var tenantId = tenantIdObj.ToString();
        
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogError("Tenant ID in HttpContext.Items is null or empty.");
            throw new InvalidOperationException(
                "Tenant ID is null or empty. This indicates a problem in TenantMiddleware.");
        }

        _logger.LogDebug("Retrieved tenant ID {TenantId} from context.", tenantId);
        return tenantId;
    }
}
