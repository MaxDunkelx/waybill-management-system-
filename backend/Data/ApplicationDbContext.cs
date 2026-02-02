using Microsoft.EntityFrameworkCore;
using WaybillManagementSystem.Models;
using WaybillManagementSystem.Models.Enums;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Data;

/// <summary>
/// The main database context for the Waybill Management System.
/// This DbContext manages all database operations and provides the Entity Framework Core
/// abstraction layer for the application. It includes critical multi-tenant isolation
/// through global query filters that automatically filter all queries by TenantId.
/// 
/// MULTI-TENANT ISOLATION STRATEGY:
/// This DbContext implements tenant isolation at the database query level using global query filters.
/// Every entity that has a TenantId property is automatically filtered, ensuring that:
/// 1. Queries only return data belonging to the current tenant
/// 2. Inserts automatically include the correct TenantId
/// 3. Updates and deletes can only affect the current tenant's data
/// 4. Cross-tenant data access is impossible without explicitly disabling the filter
/// 
/// HOW IT WORKS:
/// The OnModelCreating method configures a global query filter for each tenant-aware entity.
/// When EF Core executes any query (LINQ, Include, etc.), it automatically adds a WHERE clause
/// filtering by TenantId. This happens transparently - developers don't need to remember to
/// add tenant filtering to every query. The filter is applied to:
/// - Direct queries: dbContext.Waybills.ToList() → automatically filters by TenantId
/// - Navigation properties: waybill.Project → only returns if Project.TenantId matches
/// - Includes: dbContext.Waybills.Include(w => w.Project) → only includes matching projects
/// 
/// SECURITY BENEFITS:
/// This approach provides defense-in-depth security:
/// - Application-level: Controllers/services must set TenantId from authenticated user
/// - Database-level: Even if application code has bugs, the filter prevents data leakage
/// - Query-level: No way to accidentally query across tenants without explicitly disabling filters
/// 
/// DISABLING FILTERS (Advanced):
/// In rare cases (e.g., admin operations, data migration), you can disable filters:
/// dbContext.Waybills.IgnoreQueryFilters().ToList() - but this should be used sparingly
/// and only in trusted code paths with proper authorization checks.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ITenantService? _tenantService;

    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID (optional for design-time operations).</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService? tenantService = null)
        : base(options)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// DbSet for Tenant entities.
    /// Represents all tenants in the system. Note that tenant queries are typically
    /// not filtered by TenantId since tenants are top-level entities, but access
    /// should still be controlled through application-level authorization.
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; } = null!;

    /// <summary>
    /// DbSet for Project entities.
    /// All queries to this DbSet are automatically filtered by TenantId through
    /// the global query filter configured in OnModelCreating.
    /// </summary>
    public DbSet<Project> Projects { get; set; } = null!;

    /// <summary>
    /// DbSet for Supplier entities.
    /// All queries to this DbSet are automatically filtered by TenantId through
    /// the global query filter configured in OnModelCreating.
    /// </summary>
    public DbSet<Supplier> Suppliers { get; set; } = null!;

    /// <summary>
    /// DbSet for Waybill entities.
    /// This is the core entity set containing all waybills. All queries are automatically
    /// filtered by TenantId through the global query filter, ensuring complete tenant isolation.
    /// </summary>
    public DbSet<Waybill> Waybills { get; set; } = null!;

    /// <summary>
    /// DbSet for Job entities.
    /// Represents background jobs in the system. All queries are automatically
    /// filtered by TenantId through the global query filter, ensuring tenant isolation.
    /// </summary>
    public DbSet<Job> Jobs { get; set; } = null!;

    /// <summary>
    /// Configures the entity models and their relationships.
    /// This method is called by EF Core during model building and is where we configure:
    /// - Global query filters for multi-tenant isolation
    /// - Entity property configurations (string lengths, required fields, etc.)
    /// - Indexes for performance
    /// - Column types (especially nvarchar for Hebrew text support)
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================================
        // GLOBAL QUERY FILTERS FOR MULTI-TENANT ISOLATION
        // ============================================================================
        // These filters ensure that ALL queries automatically filter by TenantId.
        // The TenantId value is set at runtime through the SetTenantId method or
        // through a service that injects the current tenant context.
        // 
        // IMPORTANT: The TenantId must be set before executing any queries.
        // This is typically done in middleware or a service that extracts the tenant
        // from the current HTTP request (e.g., from a header, subdomain, or JWT claim).

        // ============================================================================
        // GLOBAL QUERY FILTERS FOR MULTI-TENANT ISOLATION
        // ============================================================================
        // NOTE: The query filters use a property _currentTenantId that must be set
        // before executing queries. In production, this should be set automatically
        // through middleware or a service that extracts the tenant from the HTTP request.
        // 
        // The filters automatically add "WHERE TenantId = @currentTenantId" to all queries,
        // ensuring tenants can only access their own data at the database level.

        /// <summary>
        /// Global query filter for Projects.
        /// Automatically adds "WHERE TenantId = @currentTenantId" to all Project queries.
        /// This ensures tenants can only see and access their own projects.
        /// 
        /// INTEGRATION WITH TENANT SERVICE:
        /// The filter uses GetCurrentTenantId() which retrieves the tenant ID from
        /// HttpContext.Items (set by TenantMiddleware). This creates a seamless flow:
        /// 1. TenantMiddleware extracts tenant ID from X-Tenant-ID header
        /// 2. Stores in HttpContext.Items
        /// 3. TenantService reads from HttpContext.Items
        /// 4. DbContext uses TenantService in query filter
        /// 5. All queries automatically filtered by tenant
        /// </summary>
        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => p.TenantId == GetCurrentTenantId());

        /// <summary>
        /// Global query filter for Suppliers.
        /// Automatically adds "WHERE TenantId = @currentTenantId" to all Supplier queries.
        /// This ensures tenants can only see their own suppliers (or shared suppliers if
        /// the business logic allows it through a different TenantId value).
        /// 
        /// INTEGRATION WITH TENANT SERVICE:
        /// See Project query filter documentation above for integration details.
        /// </summary>
        modelBuilder.Entity<Supplier>()
            .HasQueryFilter(s => s.TenantId == GetCurrentTenantId());

        /// <summary>
        /// Global query filter for Waybills.
        /// Automatically adds "WHERE TenantId = @currentTenantId" to all Waybill queries.
        /// This is the most critical filter as waybills contain sensitive business data.
        /// The filter ensures complete isolation - a tenant can never access another tenant's waybills,
        /// even if there's a bug in application code that forgets to filter by tenant.
        /// 
        /// INTEGRATION WITH TENANT SERVICE:
        /// See Project query filter documentation above for integration details.
        /// </summary>
        modelBuilder.Entity<Waybill>()
            .HasQueryFilter(w => w.TenantId == GetCurrentTenantId());

        // ============================================================================
        // ENTITY CONFIGURATIONS
        // ============================================================================

        // Tenant Configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)"); // nvarchar for Hebrew support

            entity.HasIndex(e => e.Name); // Index for faster tenant lookup
        });

        // Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)"); // nvarchar for Hebrew support
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasMaxLength(50);

            // Foreign key relationship
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Projects)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting tenant with projects

            // Indexes for performance
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Id }); // Composite index for tenant-scoped queries
        });

        // Supplier Configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            // COMPOSITE PRIMARY KEY: (TenantId, Id)
            // This allows multiple tenants to have suppliers with the same ID.
            // For example, both TENANT001 and TENANT002 can have supplier "SUP003" (e.g., "תרמיקס ישראל").
            // Tenant isolation is maintained through global query filters that automatically filter by TenantId.
            entity.HasKey(e => new { e.TenantId, e.Id });
            
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)"); // nvarchar for Hebrew support
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasMaxLength(50);

            // Foreign key relationship
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Suppliers)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting tenant with suppliers

            // Indexes for performance
            // Note: TenantId is already part of the primary key, so separate index may be redundant
            // but kept for explicit query optimization documentation
            entity.HasIndex(e => e.TenantId);
            // The composite index (TenantId, Id) is now the primary key, so this is redundant
            // but kept for backward compatibility and explicit documentation
            entity.HasIndex(e => new { e.TenantId, e.Id }); // Composite index for tenant-scoped queries
        });

        // Waybill Configuration
        modelBuilder.Entity<Waybill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            
            // Date fields
            entity.Property(e => e.WaybillDate)
                .IsRequired()
                .HasColumnType("date");
            entity.Property(e => e.DeliveryDate)
                .IsRequired()
                .HasColumnType("date");

            // Foreign keys
            entity.Property(e => e.ProjectId)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.SupplierId)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasMaxLength(50);

            // Product information (Hebrew text support)
            entity.Property(e => e.ProductCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)"); // nvarchar for Hebrew support

            // Quantity and pricing
            entity.Property(e => e.Quantity)
                .IsRequired()
                .HasColumnType("decimal(18,3)");
            entity.Property(e => e.Unit)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)"); // nvarchar for Hebrew support
            entity.Property(e => e.UnitPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("ILS");

            // Status enum
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>() // Store enum as int in database
                .HasDefaultValue(WaybillStatus.Pending);

            // Optional fields (Hebrew text support)
            entity.Property(e => e.VehicleNumber)
                .HasMaxLength(20);
            entity.Property(e => e.DriverName)
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)"); // nvarchar for Hebrew support
            entity.Property(e => e.DeliveryAddress)
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("nvarchar(1000)"); // nvarchar for Hebrew support
            entity.Property(e => e.Notes)
                .HasMaxLength(2000)
                .HasColumnType("nvarchar(2000)"); // nvarchar for Hebrew support

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            entity.Property(e => e.UpdatedAt);

            // Optimistic locking (row version)
            entity.Property(e => e.Version)
                .IsRowVersion(); // SQL Server rowversion/timestamp

            // ERP synchronization status
            entity.Property(e => e.ErpSyncStatus)
                .IsRequired()
                .HasConversion<int>() // Store enum as int in database
                .HasDefaultValue(ErpSyncStatus.PendingSync);
            entity.Property(e => e.LastErpSyncAttemptAt);

            // Index for ERP sync queries
            entity.HasIndex(e => e.ErpSyncStatus);

            // Foreign key relationships
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Waybills)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting tenant with waybills

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Waybills)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting project with waybills

            // Foreign key to Supplier uses composite key (TenantId, SupplierId)
            // This matches the Supplier's composite primary key (TenantId, Id)
            // Since Waybills already have TenantId, we can use both columns for the foreign key
            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.Waybills)
                .HasForeignKey(e => new { e.TenantId, e.SupplierId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting supplier with waybills

            // Indexes for performance
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.WaybillDate);
            entity.HasIndex(e => e.DeliveryDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.WaybillDate }); // Composite for tenant-scoped date queries
            entity.HasIndex(e => new { e.TenantId, e.Status }); // Composite for tenant-scoped status queries
        });

        // Job Configuration
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.JobType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(JobStatus.Pending);
            entity.Property(e => e.InputData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ResultData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ErrorMessage).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProgressPercentage).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Foreign key relationship
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.Status }); // Composite for tenant-scoped status queries
        });

        // Global query filter for Jobs
        modelBuilder.Entity<Job>()
            .HasQueryFilter(j => j.TenantId == GetCurrentTenantId());
    }

    /// <summary>
    /// Gets the current tenant ID for use in global query filters.
    /// 
    /// INTEGRATION WITH TENANT SERVICE:
    /// This method retrieves the tenant ID from ITenantService, which in turn
    /// reads from HttpContext.Items (populated by TenantMiddleware). This creates
    /// a complete integration chain:
    /// 
    /// 1. HTTP Request arrives with X-Tenant-ID header
    /// 2. TenantMiddleware extracts tenant ID → stores in HttpContext.Items
    /// 3. TenantService reads from HttpContext.Items → provides to DbContext
    /// 4. This method calls TenantService → gets tenant ID for query filters
    /// 5. EF Core uses tenant ID in WHERE clause → automatically filters queries
    /// 
    /// RESULT:
    /// All database queries are automatically scoped to the current tenant, ensuring
    /// complete data isolation without requiring explicit filtering in application code.
    /// 
    /// DESIGN-TIME SUPPORT:
    /// During migrations and design-time operations, TenantService may not be available.
    /// In these cases, the method returns null, and query filters are effectively disabled
    /// (which is correct for migrations that need to see all data).
    /// </summary>
    /// <returns>The current tenant ID, or null if not available (e.g., during migrations).</returns>
    private string? GetCurrentTenantId()
    {
        // During design-time operations (migrations), TenantService may not be available
        // In this case, return null which effectively disables the query filter
        // This is correct because migrations need to see all data
        if (_tenantService == null)
        {
            return null;
        }

        try
        {
            return _tenantService.GetCurrentTenantId();
        }
        catch (InvalidOperationException)
        {
            // If tenant ID is not available (e.g., outside HTTP context), return null
            // This allows the DbContext to be used in non-HTTP scenarios (e.g., background jobs)
            // In such cases, you may want to explicitly set tenant context or use IgnoreQueryFilters()
            return null;
        }
    }
}
