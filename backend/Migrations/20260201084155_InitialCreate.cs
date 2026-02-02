using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaybillManagementSystem.Migrations
{
    /// <summary>
    /// Initial database migration for the Waybill Management System.
    /// This migration creates the complete database schema including:
    /// - Tenant table (multi-tenant isolation)
    /// - Project table (tenant-scoped projects)
    /// - Supplier table (tenant-scoped suppliers)
    /// - Waybill table (core entity with full business logic)
    /// 
    /// MULTI-TENANT ISOLATION:
    /// All tenant-scoped tables (Projects, Suppliers, Waybills) include a TenantId column
    /// and indexes on TenantId to support efficient filtering. The ApplicationDbContext
    /// uses global query filters to automatically filter all queries by TenantId, ensuring
    /// complete data isolation at the database level.
    /// 
    /// HEBREW TEXT SUPPORT:
    /// All text columns use nvarchar (Unicode) instead of varchar to properly support
    /// Hebrew characters (Unicode range U+0590 to U+05FF). This is critical for:
    /// - Project names
    /// - Supplier names
    /// - Product names
    /// - Driver names
    /// - Delivery addresses
    /// - Notes
    /// - Units of measurement
    /// 
    /// OPTIMISTIC LOCKING:
    /// The Waybills table includes a Version column (rowversion/timestamp) for optimistic
    /// concurrency control. This prevents lost updates when multiple users modify the same
    /// waybill simultaneously.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Applies the migration, creating all tables, indexes, and constraints.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================================
            // TENANTS TABLE
            // ============================================================================
            // The Tenants table is the root of the multi-tenant hierarchy.
            // Each tenant represents a separate organization with isolated data.
            // This table does not have a TenantId because tenants are top-level entities.
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            // ============================================================================
            // PROJECTS TABLE
            // ============================================================================
            // Projects belong to a specific tenant and group waybills by project.
            // The TenantId foreign key ensures referential integrity and supports
            // the global query filter for multi-tenant isolation.
            // DeleteBehavior.Restrict prevents deleting a tenant that has projects.
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false), // nvarchar for Hebrew support
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Prevent deleting tenant with projects
                });

            // ============================================================================
            // SUPPLIERS TABLE
            // ============================================================================
            // Suppliers belong to a specific tenant (or can be shared with a special TenantId).
            // The TenantId foreign key ensures referential integrity and supports
            // the global query filter for multi-tenant isolation.
            // DeleteBehavior.Restrict prevents deleting a tenant that has suppliers.
            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false), // nvarchar for Hebrew support
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Prevent deleting tenant with suppliers
                });

            // ============================================================================
            // WAYBILLS TABLE
            // ============================================================================
            // The core entity of the system. Each waybill represents a delivery of goods
            // from a supplier to a project. The table includes:
            // - Product information (code, name, quantity, unit, pricing)
            // - Delivery details (dates, address, vehicle, driver)
            // - Status tracking (enum stored as int)
            // - Multi-tenant isolation (TenantId)
            // - Optimistic locking (Version column as rowversion)
            // 
            // FOREIGN KEYS:
            // - TenantId: Links to Tenants table (multi-tenant isolation)
            // - ProjectId: Links to Projects table (which project receives the delivery)
            // - SupplierId: Links to Suppliers table (which supplier provides the goods)
            // 
            // All foreign keys use DeleteBehavior.Restrict to prevent accidental data loss.
            migrationBuilder.CreateTable(
                name: "Waybills",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaybillDate = table.Column<DateTime>(type: "date", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "date", nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false), // nvarchar for Hebrew support
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false), // 3 decimal places for precise quantities
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false), // nvarchar for Hebrew units (e.g., מ"ק)
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false), // 2 decimal places for currency
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false), // 2 decimal places for currency
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "ILS"), // Default to Israeli Shekel
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0), // Enum stored as int (0=Pending, 1=Delivered, 2=Cancelled, 3=Disputed)
                    VehicleNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true), // Optional vehicle license plate
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true), // Optional, nvarchar for Hebrew names
                    DeliveryAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false), // nvarchar for Hebrew addresses
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true), // Optional, nvarchar for Hebrew notes
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false), // Critical for multi-tenant isolation
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true), // Nullable for newly created waybills
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true) // Optimistic locking - automatically updated by SQL Server
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waybills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Waybills_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Prevent deleting project with waybills
                    table.ForeignKey(
                        name: "FK_Waybills_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Prevent deleting supplier with waybills
                    table.ForeignKey(
                        name: "FK_Waybills_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Prevent deleting tenant with waybills
                });

            // ============================================================================
            // INDEXES FOR PERFORMANCE
            // ============================================================================
            // Indexes are critical for query performance, especially in a multi-tenant system
            // where most queries filter by TenantId. The indexes below are designed to optimize:
            // 1. Tenant-scoped queries (most common)
            // 2. Foreign key lookups
            // 3. Date range queries
            // 4. Status filtering

            // Projects: Index on TenantId for efficient tenant-scoped queries
            // WHY: The global query filter always filters by TenantId, so this index is essential
            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId");

            // Projects: Composite index on (TenantId, Id) for efficient tenant-scoped lookups
            // WHY: When querying a specific project within a tenant, this index covers the entire query
            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId_Id",
                table: "Projects",
                columns: new[] { "TenantId", "Id" });

            // Suppliers: Index on TenantId for efficient tenant-scoped queries
            // WHY: The global query filter always filters by TenantId, so this index is essential
            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers",
                column: "TenantId");

            // Suppliers: Composite index on (TenantId, Id) for efficient tenant-scoped lookups
            // WHY: When querying a specific supplier within a tenant, this index covers the entire query
            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId_Id",
                table: "Suppliers",
                columns: new[] { "TenantId", "Id" });

            // Tenants: Index on Name for efficient tenant lookup by name
            // WHY: Useful for admin operations and tenant selection
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            // Waybills: Index on DeliveryDate for date range queries
            // WHY: Common query pattern: "Get all waybills delivered between dates"
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_DeliveryDate",
                table: "Waybills",
                column: "DeliveryDate");

            // Waybills: Index on ProjectId for foreign key lookups
            // WHY: Efficiently find all waybills for a specific project
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_ProjectId",
                table: "Waybills",
                column: "ProjectId");

            // Waybills: Index on Status for status filtering
            // WHY: Common query pattern: "Get all pending waybills", "Get all delivered waybills"
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_Status",
                table: "Waybills",
                column: "Status");

            // Waybills: Index on SupplierId for foreign key lookups
            // WHY: Efficiently find all waybills from a specific supplier
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_SupplierId",
                table: "Waybills",
                column: "SupplierId");

            // Waybills: Index on TenantId for efficient tenant-scoped queries
            // WHY: The global query filter always filters by TenantId, so this index is essential
            // This is the most important index for multi-tenant isolation performance
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_TenantId",
                table: "Waybills",
                column: "TenantId");

            // Waybills: Composite index on (TenantId, Status) for tenant-scoped status queries
            // WHY: Common query pattern: "Get all pending waybills for this tenant"
            // This composite index covers both the tenant filter and status filter
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_TenantId_Status",
                table: "Waybills",
                columns: new[] { "TenantId", "Status" });

            // Waybills: Composite index on (TenantId, WaybillDate) for tenant-scoped date queries
            // WHY: Common query pattern: "Get all waybills for this tenant created in a date range"
            // This composite index covers both the tenant filter and date filter
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_TenantId_WaybillDate",
                table: "Waybills",
                columns: new[] { "TenantId", "WaybillDate" });

            // Waybills: Index on WaybillDate for date range queries (non-tenant-scoped)
            // WHY: Useful for admin operations or reports that span multiple tenants
            // NOTE: This index is less critical than the composite (TenantId, WaybillDate) index
            migrationBuilder.CreateIndex(
                name: "IX_Waybills_WaybillDate",
                table: "Waybills",
                column: "WaybillDate");
        }

        /// <summary>
        /// Rolls back the migration, dropping all tables in reverse order.
        /// Tables are dropped in reverse order of creation to respect foreign key constraints.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order to respect foreign key constraints
            migrationBuilder.DropTable(
                name: "Waybills");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
