using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.Middleware;
using WaybillManagementSystem.Services;

/// <summary>
/// Main entry point for the Waybill Management System Web API.
/// This file configures the application pipeline, services, and middleware.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// ENCODING CONFIGURATION - CRITICAL FOR HEBREW TEXT SUPPORT
// ============================================================================
/// <summary>
/// Configure UTF-8 encoding support for Hebrew and other Unicode characters.
/// WHY: By default, .NET may not properly handle Hebrew characters (right-to-left text)
/// in HTTP responses, which can cause character corruption or display issues.
/// This ensures all text encoding uses UTF-8, which fully supports Hebrew Unicode characters.
/// </summary>
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
builder.Services.Configure<WebEncoderOptions>(options =>
{
    // Configure text encoder to allow all Unicode ranges, including Hebrew (U+0590 to U+05FF)
    // This prevents ASP.NET Core from HTML-encoding Hebrew characters unnecessarily
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

// ============================================================================
// API SERVICES CONFIGURATION
// ============================================================================

/// <summary>
/// Add controllers and API-related services to the dependency injection container.
/// WHY: This enables the MVC pattern for handling HTTP requests and responses.
/// Controllers will be automatically discovered and registered.
/// 
/// JSON CONFIGURATION:
/// Configure JSON serialization options to handle enum values as strings for better
/// API compatibility. This allows:
/// - Frontend to send enum values as strings (e.g., "DISPUTED", "Delivered")
/// - Backend to deserialize string enum values (case-insensitive)
/// - Backend to serialize enum values as strings (consistent with requests)
/// 
/// WHY STRING ENUMS:
/// - More readable in API responses (strings vs numbers)
/// - Easier for frontend developers (no need to map numbers to strings)
/// - Better API documentation (Swagger shows string values)
/// - Case-insensitive parsing for robustness (handles "DISPUTED", "disputed", "Disputed")
/// </summary>
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure enum serialization/deserialization
        // Use JsonStringEnumConverter to handle enums as strings in both directions
        // This allows:
        // - Frontend to send enum values as strings (e.g., "DISPUTED", "Delivered", "Disputed") in request bodies
        // - Backend to deserialize string enum values (case-insensitive for robustness)
        // - Backend to serialize enum values as strings in responses (more readable than numbers)
        // - Backward compatibility: still accepts numeric enum values (0, 1, 2, 3)
        // 
        // IMPORTANT: namingPolicy: null means use enum name exactly as written (PascalCase: "Disputed")
        // but the converter will parse case-insensitively, so "DISPUTED", "disputed", "Disputed" all work
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                namingPolicy: null, // Use enum name as-is (PascalCase) but parse case-insensitively
                allowIntegerValues: true // Also allow numeric values (0, 1, 2, 3) for backward compatibility
            )
        );
        
        // Note: We do NOT change PropertyNamingPolicy to maintain backward compatibility
        // with existing frontend code that expects PascalCase property names
    });

/// <summary>
/// Configure API behavior options for better error handling and validation.
/// WHY: This provides consistent error responses and automatic model validation
/// for all API endpoints, improving developer experience and API reliability.
/// </summary>
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    // Automatically return 400 Bad Request for invalid model states
    options.InvalidModelStateResponseFactory = context =>
    {
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
    };
});

// ============================================================================
// SWAGGER/OPENAPI CONFIGURATION
// ============================================================================

/// <summary>
/// Add Swagger/OpenAPI documentation generation.
/// WHY: Swagger provides interactive API documentation that allows developers
/// to test endpoints directly from the browser. Essential for API development
/// and integration with frontend applications.
/// </summary>
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Waybill Management System API",
        Version = "v1",
        Description = "API for managing waybills, shipments, and logistics operations. " +
                      "Supports Hebrew text encoding for international operations. " +
                      "\n\n**⚠️ IMPORTANT - REQUIRED STEP:** " +
                      "You MUST click the 'Authorize' button (🔒) at the top right and enter a tenant ID " +
                      "(TENANT001, TENANT002, or TENANT003) before making any API requests. " +
                      "All requests require the X-Tenant-ID header, otherwise you'll get 400 Bad Request errors."
    });

    // Include XML comments in Swagger documentation
    // WHY: This allows XML documentation comments (like these) to appear in Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for X-Tenant-ID header
    // WHY: This makes it easy to set the tenant ID in Swagger UI using the "Authorize" button
    // Users can click "Authorize" and enter: TENANT001, TENANT002, or TENANT003
    c.AddSecurityDefinition("TenantIdHeader", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "X-Tenant-ID",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Tenant ID for multi-tenancy. Enter one of: TENANT001, TENANT002, TENANT003",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey
    });

    // Add document filter to apply security requirement to all operations (for Authorize button)
    // This ensures the "Authorize" button works and automatically includes X-Tenant-ID header
    c.DocumentFilter<WaybillManagementSystem.Swagger.TenantSecurityDocumentFilter>();
});

// ============================================================================
// CORS CONFIGURATION
// ============================================================================

/// <summary>
/// Configure Cross-Origin Resource Sharing (CORS) to allow requests from any origin.
/// WHY: CORS is required when the frontend (running on a different port/domain)
/// needs to make API calls to this backend. Currently set to allow all origins
/// for development. In production, this should be restricted to specific domains.
/// 
/// NOTE: For production, replace "*" with specific allowed origins:
/// builder.Services.AddCors(options => {
///     options.AddPolicy("AllowSpecificOrigins", policy => {
///         policy.WithOrigins("https://yourdomain.com")
///               .AllowAnyMethod()
///               .AllowAnyHeader();
///     });
/// });
/// </summary>
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()      // Allow requests from any origin
              .AllowAnyMethod()       // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.)
              .AllowAnyHeader();      // Allow all request headers
    });
});

// ============================================================================
// ENTITY FRAMEWORK CORE CONFIGURATION
// ============================================================================

/// <summary>
/// Register ApplicationDbContext with dependency injection.
/// WHY: Entity Framework Core provides object-relational mapping (ORM) capabilities,
/// allowing us to work with database entities as C# objects. The connection string
/// is read from appsettings.json, making it easy to configure for different environments.
/// 
/// SCOPED LIFETIME: The DbContext is registered with Scoped lifetime, meaning:
/// - One instance per HTTP request
/// - Automatically disposed at the end of each request
/// - Ensures thread-safety and proper resource management
/// - Allows the same DbContext instance to be shared across services in a single request
/// 
/// SENSITIVE DATA LOGGING: In development, we enable sensitive data logging to help
/// debug SQL queries and see the actual parameter values. This should NEVER be enabled
/// in production as it can log sensitive information like passwords or personal data.
/// 
/// SQL SERVER CONFIGURATION:
/// - CommandTimeout(30): Sets the maximum time (in seconds) to wait for a command to execute.
///   This prevents queries from hanging indefinitely and helps identify performance issues.
/// - TrustServerCertificate=True: Allows connection without validating the server certificate.
///   This is acceptable for development but should use proper certificates in production.
/// - Encrypt=True: Ensures data is encrypted in transit between the application and SQL Server.
/// </summary>
// ============================================================================
// MULTI-TENANT SERVICES CONFIGURATION
// ============================================================================

/// <summary>
/// Register IHttpContextAccessor for accessing HTTP context in services.
/// WHY: TenantService needs access to HttpContext.Items to retrieve the tenant ID
/// that was stored by TenantMiddleware. IHttpContextAccessor provides this access
/// in a way that's compatible with dependency injection.
/// 
/// SCOPED LIFETIME: Registered as Scoped to match the HTTP request lifetime.
/// </summary>
builder.Services.AddHttpContextAccessor();

/// <summary>
/// Register ITenantService for accessing the current tenant context.
/// WHY: This service provides a clean abstraction for accessing the current tenant ID
/// throughout the application. It reads from HttpContext.Items (populated by TenantMiddleware)
/// and makes the tenant ID available to services and DbContext.
/// 
/// SCOPED LIFETIME: Registered as Scoped to ensure each HTTP request has its own
/// tenant context. This prevents cross-request contamination.
/// 
/// INTEGRATION WITH DBCONTEXT:
/// ApplicationDbContext injects ITenantService and uses it in global query filters
/// to automatically filter all database queries by the current tenant ID.
/// </summary>
builder.Services.AddScoped<ITenantService, TenantService>();

/// <summary>
/// Register IWaybillValidationService for validating waybill data during import.
/// WHY: This service provides comprehensive validation of waybill data including
/// required fields, data types, business rules, and duplicate detection. It separates
/// errors (block import) from warnings (allow import but alert).
/// 
/// SCOPED LIFETIME: Registered as Scoped to ensure each HTTP request has its own
/// validation context.
/// 
/// VALIDATION RULES:
/// The service validates:
/// - Required fields (waybill_id, dates, project_id, supplier_id, etc.)
/// - Data types (dates, decimals, status enum)
/// - Business rules (quantity range, price calculation, date ordering)
/// - Duplicate detection (waybill_id + supplier_id + delivery_date)
/// 
/// ERROR VS WARNING:
/// - Errors: Block import, must be fixed (e.g., missing required field, invalid date)
/// - Warnings: Allow import but alert user (e.g., unusual quantity, status transition)
/// </summary>
builder.Services.AddScoped<IWaybillValidationService, WaybillValidationService>();

/// <summary>
/// Register RabbitMQ message publisher and consumer services.
/// WHY: These services enable event-driven architecture by publishing and consuming
/// events through RabbitMQ message broker. This decouples services and allows
/// asynchronous event processing.
/// 
/// EVENT-DRIVEN ARCHITECTURE:
/// - Publisher: Publishes events when important actions occur (e.g., waybill import)
/// - Consumer: Subscribes to events and processes them asynchronously
/// - Benefits: Decoupling, scalability, resilience, flexibility
/// 
/// SINGLETON LIFETIME: Both services are registered as Singleton because:
/// - RabbitMQ connections should be reused across requests
/// - Connection state must persist across HTTP requests
/// - No per-request state is needed
/// 
/// RABBITMQ CONFIGURATION:
/// Connection settings are read from appsettings.json:
/// - RabbitMQ:HostName (default: localhost)
/// - RabbitMQ:Port (default: 5672)
/// - RabbitMQ:UserName (default: guest)
/// - RabbitMQ:Password (default: guest)
/// - RabbitMQ:VirtualHost (default: /)
/// 
/// ERROR HANDLING:
/// - Publisher: Errors are logged but don't throw exceptions (fire-and-forget)
/// - Consumer: Errors are logged, messages are requeued for retry
/// - Connection recovery: Automatic reconnection on failure
/// 
/// </summary>
builder.Services.AddSingleton<IMessagePublisher, MessagePublisher>();
builder.Services.AddSingleton<IMessageConsumer, MessageConsumer>();

/// <summary>
/// Register WaybillEventConsumer as a hosted service.
/// WHY: This service runs in the background and continuously listens for waybill
/// events from RabbitMQ. It starts when the application starts and stops gracefully
/// when the application shuts down.
/// 
/// HOSTED SERVICE:
/// BackgroundService runs continuously in the background, independent of HTTP requests.
/// It's the recommended way to run long-running background tasks in ASP.NET Core.
/// 
/// LIFECYCLE:
/// - Starts automatically when application starts
/// - Runs continuously until application shuts down
/// - Stops gracefully on shutdown
/// 
/// </summary>
builder.Services.AddHostedService<WaybillEventConsumer>();

/// <summary>
/// Register IDistributedLockService for distributed locking.
/// WHY: This service provides distributed locking capabilities to ensure that certain
/// operations can only be executed by one process/user at a time. This is critical
/// for preventing concurrent execution of expensive operations like report generation.
/// 
/// SINGLETON LIFETIME: Registered as Singleton because:
/// - The lock dictionary needs to be shared across all requests
/// - Lock state must persist across HTTP requests
/// - No per-request state is needed
/// 
/// IMPLEMENTATION:
/// The current implementation uses in-memory locking (DistributedLockService), which
/// is suitable for single-server deployments. For multi-server deployments, you should
/// use a Redis-based implementation to ensure locks work across multiple application
/// instances.
/// 
/// UPGRADE PATH:
/// To upgrade to Redis-based locking:
/// 1. Create RedisDistributedLockService implementing IDistributedLockService
/// 2. Use Redis SETNX (SET if Not eXists) with expiration
/// 3. Register RedisDistributedLockService here instead of DistributedLockService
/// 4. Ensure Redis connection is configured in appsettings.json
/// </summary>
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

/// <summary>
/// Register IWaybillService for querying waybill data.
/// WHY: This service provides methods for retrieving waybill data with filtering,
/// pagination, and search capabilities. All queries automatically filter by tenant
/// through the global query filter in ApplicationDbContext.
/// 
/// SCOPED LIFETIME: Registered as Scoped to ensure each HTTP request has its own
/// service context. This is important for thread safety and proper resource management.
/// 
/// QUERY FEATURES:
/// The service supports:
/// - Filtering by date range, status, project, supplier, product code
/// - Hebrew text search in project name, supplier name, product name
/// - Pagination for efficient data retrieval
/// - Automatic tenant isolation via global query filters
/// 
/// PERFORMANCE:
/// All queries use IQueryable for optimal SQL generation and proper use of database
/// indexes. Filters are applied at the database level, not in memory.
/// </summary>
builder.Services.AddScoped<IWaybillService, WaybillService>();

/// <summary>
/// Register IWaybillImportService for importing waybill data from CSV files.
/// WHY: This service handles parsing of CSV files containing waybill data, converting
/// CSV rows into structured DTOs. It properly handles UTF-8 encoding for Hebrew text
/// and provides comprehensive error reporting.
/// 
/// SCOPED LIFETIME: Registered as Scoped to ensure each HTTP request has its own
/// import context. This is important for thread safety and proper resource management.
/// 
/// CSV PARSING:
/// The service uses CsvHelper library configured for:
/// - UTF-8 encoding (Hebrew text support)
/// - Flexible date parsing (multiple formats)
/// - Error collection and reporting
/// - Best-effort processing (continues on errors)
/// 
/// VALIDATION INTEGRATION:
/// The service now integrates with IWaybillValidationService to validate each parsed row.
/// Only rows that pass validation are included in the result. Validation errors and
/// warnings are collected and returned in the ImportResultDto.
/// 
/// DATABASE OPERATIONS:
/// The service now handles database operations including:
/// - Upsert logic (update if exists, create if not)
/// - Automatic creation of Projects and Suppliers
/// - Transaction management
/// - Error handling and recovery
/// </summary>
builder.Services.AddScoped<IWaybillImportService, WaybillImportService>();

/// <summary>
/// Register IJobService for background job management.
/// WHY: This service provides methods for creating, querying, and managing background jobs.
/// It handles job creation, status updates, and result storage.
/// 
/// SCOPED LIFETIME: Registered as Scoped to ensure each HTTP request has its own
/// service context. This is important for thread safety and proper resource management.
/// 
/// JOB PROCESSING:
/// Jobs are created with PENDING status and processed by background workers.
/// The service updates job status as jobs are processed (PENDING → PROCESSING → COMPLETED/FAILED).
/// </summary>
builder.Services.AddScoped<IJobService, JobService>();

/// <summary>
/// Register JobProcessorBackgroundService as a hosted service.
/// WHY: This service runs in the background and periodically processes jobs with PENDING status.
/// It executes jobs based on job type (e.g., CSV_IMPORT) and updates job status.
/// 
/// HOSTED SERVICE:
/// BackgroundService runs continuously in the background, independent of HTTP requests.
/// It processes jobs every 10 seconds, handling up to 5 jobs per cycle.
/// </summary>
builder.Services.AddHostedService<JobProcessorBackgroundService>();

/// <summary>
/// Register HttpClient for ERP integration service.
/// WHY: The ERP integration service needs an HttpClient to make HTTP requests
/// to the Priority ERP system. HttpClient is registered as a typed client
/// to ensure proper lifecycle management and configuration.
/// 
/// HTTP CLIENT CONFIGURATION:
/// - Timeout: 30 seconds (configurable)
/// - Base address: Configured via ErpIntegration:EndpointUrl in appsettings.json
/// - Default: http://localhost:5001/api/MockErp/sync-waybill (mock endpoint)
/// 
/// LIFETIME:
/// HttpClient is registered as Scoped to ensure proper disposal and prevent
/// socket exhaustion. The HttpClientFactory manages the underlying HttpMessageHandler
/// lifecycle efficiently.
/// </summary>
builder.Services.AddHttpClient<IErpIntegrationService, ErpIntegrationService>();

/// <summary>
/// Register IErpIntegrationService for Priority ERP synchronization.
/// WHY: This service synchronizes waybill data with the Priority ERP system,
/// implementing retry logic with exponential backoff and circuit breaker pattern.
/// 
/// SCOPED LIFETIME: Registered as Scoped to ensure each HTTP request has its own
/// service context. This is important for thread safety and proper resource management.
/// 
/// FEATURES:
/// - Retry logic with exponential backoff (1s, 2s, 4s delays)
/// - Circuit breaker pattern to prevent cascading failures
/// - Sync status tracking (PENDING_SYNC, SYNCED, SYNC_FAILED)
/// - Mock ERP endpoint for testing (10% failure rate simulation)
/// 
/// CONFIGURATION:
/// ERP endpoint URL is configured in appsettings.json:
/// - ErpIntegration:EndpointUrl (defaults to mock endpoint)
/// </summary>
builder.Services.AddScoped<IErpIntegrationService, ErpIntegrationService>();

/// <summary>
/// Register ErpSyncBackgroundService as a hosted service.
/// WHY: This service runs in the background and periodically processes waybills
/// with PENDING_SYNC status, attempting to synchronize them with the Priority ERP system.
/// 
/// HOSTED SERVICE:
/// BackgroundService runs continuously in the background, independent of HTTP requests.
/// It processes waybills every 30 seconds, handling up to 10 waybills per cycle.
/// 
/// LIFECYCLE:
/// - Starts automatically when application starts
/// - Runs continuously until application shuts down
/// - Stops gracefully on shutdown
/// 
/// PROCESSING:
/// The service queries waybills with PENDING_SYNC status and calls
/// ErpIntegrationService.SyncWaybillAsync() for each waybill. The ERP service
/// handles retry logic and updates sync status based on results.
/// </summary>
builder.Services.AddHostedService<ErpSyncBackgroundService>();

/// <summary>
/// Register Redis distributed cache for caching frequently accessed data.
/// WHY: Redis provides distributed caching capabilities, allowing cache to be shared
/// across multiple application instances. This improves performance by reducing database queries.
/// 
/// CONFIGURATION:
/// Redis connection string is configured in appsettings.json:
/// - Redis:ConnectionString (default: localhost:6379)
/// - Redis:InstanceName (default: WaybillManagement)
/// 
/// FALLBACK:
/// If Redis is unavailable, the application will fall back to in-memory caching
/// (registered below) for single-instance deployments.
/// </summary>
builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    var instanceName = builder.Configuration["Redis:InstanceName"] ?? "WaybillManagement";
    options.Configuration = connectionString;
    options.InstanceName = instanceName;
});

/// <summary>
/// Register in-memory cache as fallback when Redis is unavailable.
/// WHY: Provides caching capabilities even when Redis is not configured or unavailable.
/// This is useful for single-instance deployments or development environments.
/// 
/// LIMITATIONS:
/// - Not distributed (only works within a single application instance)
/// - Memory is limited (may evict entries under memory pressure)
/// 
/// USAGE:
/// The MemoryCacheService uses this IMemoryCache for in-memory caching.
/// For production multi-instance deployments, RedisCacheService should be used.
/// </summary>
builder.Services.AddMemoryCache();

/// <summary>
/// Register ICacheService for caching frequently accessed data.
/// WHY: This service provides a unified interface for caching, with implementations
/// for both Redis (distributed) and in-memory (fallback) caching.
/// 
/// IMPLEMENTATION SELECTION:
/// The service tries to use RedisCacheService (distributed) if Redis is available,
/// otherwise falls back to MemoryCacheService (in-memory).
/// 
/// CACHE STRATEGY:
/// - Cache keys: `waybill:summary:{tenantId}:{dateRange}`, `supplier:summary:{tenantId}:{supplierId}`
/// - TTL: 5 minutes (configurable via Cache:DefaultTtlMinutes in appsettings.json)
/// - Invalidation: On waybill import, status update, or waybill update
/// - Tenant-scoped: All cache keys include tenant ID for isolation
/// </summary>
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ============================================================================
// ENTITY FRAMEWORK CORE CONFIGURATION
// ============================================================================

/// <summary>
/// Register ApplicationDbContext with dependency injection.
/// WHY: Entity Framework Core provides object-relational mapping (ORM) capabilities,
/// allowing us to work with database entities as C# objects. The connection string
/// is read from appsettings.json, making it easy to configure for different environments.
/// 
/// SCOPED LIFETIME: The DbContext is registered with Scoped lifetime, meaning:
/// - One instance per HTTP request
/// - Automatically disposed at the end of each request
/// - Ensures thread-safety and proper resource management
/// - Allows the same DbContext instance to be shared across services in a single request
/// 
/// TENANT INTEGRATION:
/// ApplicationDbContext injects ITenantService (also Scoped) and uses it in global
/// query filters to automatically filter all queries by the current tenant ID. This
/// ensures complete data isolation at the database level.
/// 
/// SENSITIVE DATA LOGGING: In development, we enable sensitive data logging to help
/// debug SQL queries and see the actual parameter values. This should NEVER be enabled
/// in production as it can log sensitive information like passwords or personal data.
/// 
/// SQL SERVER CONFIGURATION:
/// - CommandTimeout(30): Sets the maximum time (in seconds) to wait for a command to execute.
///   This prevents queries from hanging indefinitely and helps identify performance issues.
/// - TrustServerCertificate=True: Allows connection without validating the server certificate.
///   This is acceptable for development but should use proper certificates in production.
/// - Encrypt=True: Ensures data is encrypted in transit between the application and SQL Server.
/// </summary>
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Set command timeout to 30 seconds
            // WHY: Prevents queries from hanging indefinitely and helps identify performance issues
            sqlOptions.CommandTimeout(30);
        });

    // Enable sensitive data logging in development only
    // WHY: Helps debug SQL queries by showing actual parameter values
    // WARNING: Never enable this in production as it can log sensitive information
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors(); // Provides more detailed error messages for debugging
    }
});

// ============================================================================
// APPLICATION BUILD AND MIDDLEWARE PIPELINE
// ============================================================================

var app = builder.Build();

/// <summary>
/// Configure the HTTP request processing pipeline.
/// The order of middleware matters - each middleware processes the request in sequence.
/// </summary>

// ============================================================================
// DEVELOPMENT-ONLY MIDDLEWARE
// ============================================================================

/// <summary>
/// Enable Swagger UI and OpenAPI endpoint in development environment.
/// WHY: Swagger UI provides an interactive interface to test API endpoints.
/// This should only be enabled in development for security reasons.
/// </summary>
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Waybill Management System API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI as the root page
        
        // Enable persistent authorization - tenant ID will persist across page refreshes
        // This stores the authorization in browser's localStorage
        // When user clicks "Authorize" and enters tenant ID, it will be remembered
        // When user clicks "Logout", it will be cleared
        c.ConfigObject.PersistAuthorization = true;
    });
}

// ============================================================================
// CORS MIDDLEWARE (MUST BE BEFORE TENANT MIDDLEWARE FOR OPTIONS REQUESTS)
// ============================================================================

/// <summary>
/// Apply CORS policy to allow cross-origin requests.
/// WHY: This must be placed early in the pipeline to handle preflight OPTIONS requests
/// before the tenant middleware. CORS preflight requests don't include custom headers,
/// so they need to be allowed before tenant validation.
/// </summary>
app.UseCors("AllowAll");

// ============================================================================
// MULTI-TENANT MIDDLEWARE
// ============================================================================

/// <summary>
/// Add TenantMiddleware to extract and validate tenant ID from HTTP requests.
/// WHY: This middleware is critical for multi-tenant isolation. It:
/// 1. Extracts tenant ID from X-Tenant-ID HTTP header
/// 2. Validates that tenant ID is present and not empty
/// 3. Stores tenant ID in HttpContext.Items for use by services and DbContext
/// 4. Returns 400 Bad Request if tenant ID is missing
/// 
/// PLACEMENT:
/// This middleware must be placed AFTER CORS but BEFORE routing to ensure
/// tenant context is available to all downstream components. The order matters:
/// 1. CORS (handles preflight OPTIONS requests)
/// 2. TenantMiddleware (extracts tenant ID)
/// 3. HTTPS Redirection
/// 4. Routing
/// 5. Authorization
/// 6. Controllers
/// 
/// INTEGRATION WITH EF CORE:
/// The tenant ID stored by this middleware is used by:
/// 1. TenantService (reads from HttpContext.Items)
/// 2. ApplicationDbContext (uses TenantService in global query filters)
/// 3. All database queries (automatically filtered by tenant ID)
/// 
/// This creates complete tenant isolation:
/// - Request level: Middleware validates tenant ID
/// - Service level: TenantService provides tenant context
/// - Database level: Global query filters enforce isolation
/// 
/// SECURITY:
/// By placing this middleware early in the pipeline, we ensure that no request
/// can proceed without a valid tenant ID. This prevents accidental cross-tenant
/// data access at the application level, while EF Core global query filters
/// provide defense-in-depth at the database level.
/// </summary>
app.UseMiddleware<TenantMiddleware>();

// ============================================================================
// SECURITY MIDDLEWARE
// ============================================================================

/// <summary>
/// Redirect HTTP requests to HTTPS (only in production or when HTTPS is configured).
/// WHY: HTTPS encrypts data in transit, protecting sensitive waybill information
/// and user credentials. This middleware automatically redirects HTTP to HTTPS.
/// In development with HTTP-only, we skip this to avoid SSL errors.
/// </summary>
// Only use HTTPS redirection in production or when HTTPS endpoints are configured
// This prevents ERR_SSL_PROTOCOL_ERROR when accessing HTTP-only endpoints
// Note: Access the API via http://localhost:5001 (not https://) when running HTTP-only
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS is already applied earlier in the pipeline (before TenantMiddleware)

// ============================================================================
// ROUTING AND ENDPOINTS
// ============================================================================

/// <summary>
/// Enable routing to map incoming requests to controller actions.
/// WHY: Routing determines which controller and action method handles each HTTP request
/// based on the URL pattern and HTTP method.
/// </summary>
app.UseRouting();

/// <summary>
/// Enable authorization middleware (currently not configured, but ready for future use).
/// WHY: When authentication/authorization is added, this middleware will enforce
/// security policies on protected endpoints.
/// </summary>
// app.UseAuthorization();

/// <summary>
/// Map controller endpoints.
/// WHY: This connects the routing system to our controllers, allowing API endpoints
/// defined in Controllers/ to be accessible via HTTP requests.
/// </summary>
app.MapControllers();

// ============================================================================
// DATABASE MIGRATIONS (AUTOMATIC)
// ============================================================================

/// <summary>
/// Apply database migrations automatically on application startup.
/// WHY: This ensures the database schema is always up-to-date without requiring
/// manual intervention. When the application starts, it will:
/// 1. Check if the database exists (creates it if not)
/// 2. Check which migrations have been applied (via __EFMigrationsHistory table)
/// 3. Apply any pending migrations automatically
/// 4. Start the application only if migrations succeed
/// 
/// PLACEMENT:
/// This code runs AFTER all services are registered and configured, but BEFORE
/// the web server starts listening. This ensures:
/// - All dependencies (DbContext, services) are available
/// - Database is ready before any HTTP requests are processed
/// - Application fails fast if migrations fail (better than starting with wrong schema)
/// 
/// SAFETY:
/// - Idempotent: Safe to run multiple times (only applies pending migrations)
/// - Won't delete data or re-run already applied migrations
/// - Won't modify existing tables unless a new migration requires it
/// - If migrations fail, application won't start (fail-fast approach)
/// 
/// WHEN IT RUNS:
/// - On every application startup
/// - Before the web server starts listening
/// - In all environments (Development, Production, Docker)
/// 
/// BENEFITS:
/// - No manual "dotnet ef database update" required
/// - Works in Docker containers (no .NET SDK needed on host)
/// - Consistent database schema across all environments
/// - Interviewers can run with single command: docker-compose up
/// - Reduces setup complexity and potential errors
/// 
/// ERROR HANDLING:
/// If migrations fail, the exception is logged and re-thrown, preventing the
/// application from starting. This is intentional - it's better to fail immediately
/// with a clear error message than to start with an incorrect database schema.
/// </summary>
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("==========================================");
        logger.LogInformation("Applying database migrations...");
        logger.LogInformation("==========================================");
        
        // Apply all pending migrations
        // This will:
        // 1. Create database if it doesn't exist
        // 2. Create __EFMigrationsHistory table if it doesn't exist
        // 3. Apply any migrations that haven't been applied yet
        // 4. Do nothing if all migrations are already applied
        dbContext.Database.Migrate();
        
        logger.LogInformation("==========================================");
        logger.LogInformation("Database migrations applied successfully");
        logger.LogInformation("==========================================");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "==========================================");
        logger.LogError(ex, "ERROR: Failed to apply database migrations");
        logger.LogError(ex, "Application will not start until migrations succeed");
        logger.LogError(ex, "==========================================");
        throw; // Fail-fast: don't start if migrations fail
    }
}

// ============================================================================
// APPLICATION STARTUP
// ============================================================================

/// <summary>
/// Start the web server and begin listening for HTTP requests.
/// WHY: This is the final step that actually starts the Kestrel web server
/// and makes the API available on the configured ports (typically https://localhost:5001
/// and http://localhost:5000).
/// 
/// NOTE: Database migrations are applied before this point, so the database
/// schema is guaranteed to be up-to-date when the server starts.
/// </summary>
app.Run();
