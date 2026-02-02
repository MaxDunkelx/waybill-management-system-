# Database Schema Documentation

Complete documentation of the Waybill Management System database schema, including all tables, columns, indexes, relationships, and constraints.

## Schema Overview

The database schema is designed for a multi-tenant system with strict data isolation. All entities (except Tenant) include a `TenantId` foreign key, and global query filters ensure automatic tenant-scoped queries.

## Tables

### 1. Tenants

The top-level entity representing construction companies (tenants) in the system.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `varchar(50)` | PRIMARY KEY, NOT NULL | Unique tenant identifier (e.g., "TENANT001") |
| `Name` | `varchar(200)` | NOT NULL | Display name of the tenant organization |
| `CreatedAt` | `datetime` | NOT NULL | Timestamp when tenant was created |

**Indexes:**
- Primary Key (clustered): `Id`

**Relationships:**
- One-to-Many with `Projects` (via `Project.TenantId`)
- One-to-Many with `Suppliers` (via `Supplier.TenantId`)
- One-to-Many with `Waybills` (via `Waybill.TenantId`)

**Notes:**
- This is the root entity for multi-tenant isolation
- Tenant IDs are typically uppercase strings (e.g., "TENANT001", "TENANT002")
- Created automatically during CSV import if tenant doesn't exist

---

### 2. Projects

Represents construction projects within a tenant's organization.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `varchar(50)` | PRIMARY KEY, NOT NULL | Unique project identifier (e.g., "PRJ001") |
| `TenantId` | `varchar(50)` | FOREIGN KEY, NOT NULL | Reference to `Tenant.Id` |
| `Name` | `nvarchar(500)` | NOT NULL | Project name (Hebrew text supported) |
| `CreatedAt` | `datetime` | NOT NULL | Timestamp when project was created |

**Indexes:**
- Primary Key (clustered): `Id`
- Index: `TenantId` (for tenant-scoped queries)
- Index: `Name` (for search operations)

**Foreign Keys:**
- `TenantId` → `Tenant.Id` (ON DELETE RESTRICT)

**Relationships:**
- Many-to-One with `Tenant` (via `TenantId`)
- One-to-Many with `Waybills` (via `Waybill.ProjectId`)

**Notes:**
- Projects are tenant-scoped and cannot be shared across tenants
- Project names are typically in Hebrew
- Created automatically during CSV import if project doesn't exist
- Global query filter: `WHERE TenantId = @currentTenantId`

---

### 3. Suppliers

Represents suppliers/vendors that provide goods to projects. Uses composite primary key to allow same supplier ID across tenants.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `varchar(50)` | PRIMARY KEY (part 1), NOT NULL | Supplier identifier (e.g., "SUP001") |
| `TenantId` | `varchar(50)` | PRIMARY KEY (part 2), FOREIGN KEY, NOT NULL | Reference to `Tenant.Id` |
| `Name` | `nvarchar(500)` | NOT NULL | Supplier name (Hebrew text supported) |
| `CreatedAt` | `datetime` | NOT NULL | Timestamp when supplier was created |

**Indexes:**
- Primary Key (clustered, composite): `(TenantId, Id)`
- Index: `TenantId` (redundant but explicit for documentation)

**Foreign Keys:**
- `TenantId` → `Tenant.Id` (ON DELETE RESTRICT)

**Relationships:**
- Many-to-One with `Tenant` (via `TenantId`)
- One-to-Many with `Waybills` (via composite foreign key `(TenantId, SupplierId)`)

**Notes:**
- **Composite Primary Key**: `(TenantId, Id)` allows multiple tenants to have suppliers with the same ID
  - Example: TENANT001 can have `(TENANT001, SUP003)` = "תרמיקס ישראל"
  - Example: TENANT002 can have `(TENANT002, SUP003)` = "תרמיקס ישראל"
  - Both are stored as separate records, maintaining tenant isolation
- Supplier names are typically in Hebrew
- Created automatically during CSV import if supplier doesn't exist
- Global query filter: `WHERE TenantId = @currentTenantId`

---

### 4. Waybills

The core entity representing delivery waybills. Tracks all deliveries from suppliers to projects.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `varchar(100)` | PRIMARY KEY, NOT NULL | Unique waybill identifier (e.g., "WB-2024-001") |
| `TenantId` | `varchar(50)` | FOREIGN KEY, NOT NULL | Reference to `Tenant.Id` |
| `ProjectId` | `varchar(50)` | FOREIGN KEY, NOT NULL | Reference to `Project.Id` |
| `SupplierId` | `varchar(50)` | FOREIGN KEY (part 1), NOT NULL | Part of composite FK to `Supplier` |
| `WaybillDate` | `date` | NOT NULL | Date when waybill was issued |
| `DeliveryDate` | `date` | NOT NULL | Date when goods were/will be delivered |
| `ProductCode` | `varchar(50)` | NOT NULL | Product code (e.g., "B30", "B40") |
| `ProductName` | `nvarchar(500)` | NOT NULL | Product name (Hebrew text supported) |
| `Quantity` | `decimal(18,3)` | NOT NULL | Quantity delivered (supports fractional values) |
| `Unit` | `nvarchar(20)` | NOT NULL | Unit of measurement (Hebrew, e.g., "מ\"ק") |
| `UnitPrice` | `decimal(18,2)` | NOT NULL | Price per unit |
| `TotalAmount` | `decimal(18,2)` | NOT NULL | Total amount (Quantity × UnitPrice) |
| `Currency` | `varchar(3)` | NOT NULL, DEFAULT 'ILS' | Currency code (typically "ILS") |
| `Status` | `int` | NOT NULL, DEFAULT 0 | Waybill status (0=Pending, 1=Delivered, 2=Cancelled, 3=Disputed) |
| `VehicleNumber` | `varchar(20)` | NULL | License plate number (optional) |
| `DriverName` | `nvarchar(200)` | NULL | Driver name (Hebrew, optional) |
| `DeliveryAddress` | `nvarchar(1000)` | NOT NULL | Delivery address (Hebrew) |
| `Notes` | `nvarchar(2000)` | NULL | Additional notes (Hebrew, optional) |
| `CreatedAt` | `datetime` | NOT NULL | Timestamp when waybill was created |
| `UpdatedAt` | `datetime` | NULL | Timestamp when waybill was last updated |
| `Version` | `rowversion` | NULL | Row version for optimistic concurrency control |

**Indexes:**
- Primary Key (clustered): `Id`
- Index: `TenantId` (for tenant-scoped queries)
- Index: `ProjectId` (for project filtering)
- Index: `SupplierId` (for supplier filtering)
- Index: `WaybillDate` (for date range filtering)
- Index: `DeliveryDate` (for delivery date filtering)
- Index: `Status` (for status filtering)
- Composite Index: `(TenantId, WaybillDate)` (for tenant-scoped date queries)
- Composite Index: `(TenantId, Status)` (for tenant-scoped status queries)

**Foreign Keys:**
- `TenantId` → `Tenant.Id` (ON DELETE RESTRICT)
- `ProjectId` → `Project.Id` (ON DELETE RESTRICT)
- `(TenantId, SupplierId)` → `Supplier(TenantId, Id)` (ON DELETE RESTRICT, composite foreign key)

**Relationships:**
- Many-to-One with `Tenant` (via `TenantId`)
- Many-to-One with `Project` (via `ProjectId`)
- Many-to-One with `Supplier` (via composite foreign key `(TenantId, SupplierId)`)

**Business Rules (Enforced in Application Layer):**
- `Quantity` must be between 0.5 and 50 cubic meters
- `DeliveryDate` cannot be before `WaybillDate`
- `TotalAmount` should equal `Quantity × UnitPrice` (with tolerance for rounding)
- Status transitions must follow business rules (PENDING → DELIVERED/CANCELLED, DELIVERED → DISPUTED)

**Notes:**
- **Primary Isolation Entity**: Waybills are the primary entity for tenant isolation
- **Optimistic Locking**: `Version` field (rowversion) prevents concurrent update conflicts
- **Hebrew Text Support**: ProductName, Unit, DriverName, DeliveryAddress, Notes use `nvarchar` for Unicode support
- **Upsert Logic**: Waybills are matched on `(Id, SupplierId, DeliveryDate)` for upsert operations
- Global query filter: `WHERE TenantId = @currentTenantId`

---

## Index Strategy

### Purpose of Indexes

1. **Tenant Isolation**: All `TenantId` indexes ensure efficient tenant-scoped queries
2. **Query Performance**: Indexes on frequently filtered fields (Status, Dates, ProjectId, SupplierId)
3. **Composite Indexes**: Optimize queries that filter by tenant + another field (e.g., tenant + date range)

### Index Usage Patterns

- **List Waybills**: Uses `TenantId` index + optional `Status`, `WaybillDate`, `ProjectId`, `SupplierId` indexes
- **Date Range Queries**: Uses composite index `(TenantId, WaybillDate)`
- **Status Filtering**: Uses composite index `(TenantId, Status)`
- **Project Waybills**: Uses `ProjectId` index (already tenant-scoped through global filter)
- **Supplier Waybills**: Uses `SupplierId` index (already tenant-scoped through global filter)

## Tenant Isolation Implementation

### Global Query Filters

The `ApplicationDbContext` configures global query filters for all tenant-scoped entities:

```csharp
modelBuilder.Entity<Project>()
    .HasQueryFilter(p => p.TenantId == GetCurrentTenantId());

modelBuilder.Entity<Supplier>()
    .HasQueryFilter(s => s.TenantId == GetCurrentTenantId());

modelBuilder.Entity<Waybill>()
    .HasQueryFilter(w => w.TenantId == GetCurrentTenantId());
```

### How It Works

1. **Tenant ID Extraction**: `TenantMiddleware` extracts tenant ID from `X-Tenant-ID` header
2. **Context Storage**: Tenant ID stored in `HttpContext.Items`
3. **Service Access**: `TenantService.GetCurrentTenantId()` reads from context
4. **Query Filter**: `ApplicationDbContext.GetCurrentTenantId()` provides tenant ID to EF Core
5. **Automatic Filtering**: EF Core automatically adds `WHERE TenantId = @tenantId` to all queries

### Benefits

- **Automatic**: No need to manually filter in every query
- **Defense-in-Depth**: Even if application code forgets filtering, database enforces isolation
- **Performance**: Indexes on `TenantId` ensure efficient filtered queries
- **Security**: Impossible to accidentally query across tenants

## Data Types and Constraints

### String Types

- **`varchar(n)`**: Used for IDs, codes, and ASCII text (e.g., `Id`, `ProductCode`, `Currency`)
- **`nvarchar(n)`**: Used for Hebrew text fields (e.g., `Name`, `ProductName`, `DeliveryAddress`)
  - Supports Unicode Hebrew characters (U+0590 to U+05FF)
  - Takes 2 bytes per character (vs 1 for varchar)

### Numeric Types

- **`decimal(18,3)`**: Used for quantities (supports fractional values like 12.5 cubic meters)
- **`decimal(18,2)`**: Used for currency amounts (2 decimal places for ILS precision)

### Date Types

- **`date`**: Used for waybill and delivery dates (no time component)
- **`datetime`**: Used for audit timestamps (CreatedAt, UpdatedAt)

### Special Types

- **`int`**: Used for Status enum (0=Pending, 1=Delivered, 2=Cancelled, 3=Disputed)
- **`rowversion`**: Used for optimistic concurrency control (automatically updated by SQL Server)

## Audit Fields

All entities include audit fields for tracking and compliance:

- **`CreatedAt`**: Automatically set when entity is created (required)
- **`UpdatedAt`**: Automatically updated when entity is modified (nullable, set on updates)
- **`Version`**: Row version for optimistic locking (Waybill only, automatically updated by SQL Server)

## Foreign Key Constraints

All foreign keys use `ON DELETE RESTRICT` to prevent:
- Deleting a tenant that has projects, suppliers, or waybills
- Deleting a project that has waybills
- Deleting a supplier that has waybills

This ensures data integrity and prevents orphaned records.

## Composite Key Strategy

### Supplier Composite Key

Suppliers use a composite primary key `(TenantId, Id)` to allow:
- Multiple tenants to have suppliers with the same ID
- Realistic multi-tenant scenarios (shared suppliers)
- Proper tenant isolation through global query filters

### Waybill Composite Foreign Key

Waybills reference Suppliers using a composite foreign key `(TenantId, SupplierId)` → `Supplier(TenantId, Id)` to:
- Match the Supplier's composite primary key
- Ensure referential integrity
- Maintain tenant isolation

## Migration History

The database schema is managed through EF Core migrations:

1. **InitialCreate**: Creates all tables, indexes, and foreign keys
2. **ChangeSupplierToCompositeKey**: Updates Supplier to use composite primary key

To view migration history:
```bash
cd backend
dotnet ef migrations list
```

To apply migrations:
```bash
dotnet ef database update
```

## Schema Validation

The schema is validated through:
- EF Core model validation on application startup
- Foreign key constraints at database level
- Global query filters ensuring tenant isolation
- Business rule validation in application layer

## Performance Considerations

1. **Indexes**: Strategic indexes on frequently queried fields
2. **Composite Indexes**: Optimize tenant-scoped queries
3. **Query Filters**: Global filters ensure efficient tenant-scoped queries
4. **Hebrew Text**: `nvarchar` ensures proper Unicode support
5. **Decimal Precision**: Appropriate precision for quantities and currency

## Future Considerations

### Soft Delete
Currently not implemented, but schema could be extended with:
- `IsDeleted` bit field
- `DeletedAt` datetime field
- Modified global query filters to exclude deleted records

### Additional Indexes
Could add indexes for:
- Full-text search on Hebrew text fields
- Composite indexes for complex query patterns
- Covering indexes for frequently accessed queries
