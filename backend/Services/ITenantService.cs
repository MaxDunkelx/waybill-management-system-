namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for accessing the current tenant context.
/// 
/// PURPOSE:
/// This service provides a clean abstraction for accessing the current tenant ID
/// throughout the application. It abstracts away the details of how the tenant ID
/// is stored (HttpContext.Items) and provides a consistent API for services that
/// need tenant context.
/// 
/// USAGE:
/// Services that need to know the current tenant can inject ITenantService and
/// call GetCurrentTenantId(). This is especially important for:
/// - ApplicationDbContext (for global query filters)
/// - Business logic services that need tenant context
/// - Controllers that need to validate tenant access
/// 
/// IMPLEMENTATION:
/// The TenantService implementation reads from HttpContext.Items, which is populated
/// by the TenantMiddleware. This ensures that the tenant ID is available throughout
/// the request pipeline.
/// 
/// THREAD SAFETY:
/// HttpContext.Items is scoped to the current HTTP request, so it's safe to use
/// in a scoped service. Each request has its own HttpContext instance.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current tenant ID from the HTTP request context.
    /// 
    /// RETURNS:
    /// The tenant ID that was extracted by TenantMiddleware from the X-Tenant-ID header.
    /// 
    /// THROWS:
    /// InvalidOperationException if tenant ID is not available (should not happen
    /// if TenantMiddleware is properly configured in the pipeline).
    /// 
    /// USAGE IN DBCONTEXT:
    /// The ApplicationDbContext uses this method in its global query filters to
    /// automatically filter all queries by the current tenant ID. This ensures
    /// complete data isolation without requiring explicit filtering in every query.
    /// </summary>
    /// <returns>The current tenant ID.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if tenant ID is not available in the current context.
    /// This typically indicates that TenantMiddleware is not configured or
    /// the request bypassed the middleware.
    /// </exception>
    string GetCurrentTenantId();
}
