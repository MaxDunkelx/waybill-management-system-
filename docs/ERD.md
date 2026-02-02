# Database Entity Relationship Diagram (ERD)

This document provides a visual representation of the database schema for the Waybill Management System.

## Entity Relationship Diagram

```mermaid
erDiagram
    TENANT ||--o{ PROJECT : "has"
    TENANT ||--o{ SUPPLIER : "has"
    TENANT ||--o{ WAYBILL : "has"
    
    PROJECT ||--o{ WAYBILL : "contains"
    SUPPLIER ||--o{ WAYBILL : "delivers"
    
    TENANT {
        string Id PK "Primary Key"
        string Name "nvarchar(200)"
        datetime CreatedAt "Audit Field"
    }
    
    PROJECT {
        string Id PK "Primary Key"
        string TenantId FK "Foreign Key to Tenant"
        string Name "nvarchar(500) - Hebrew"
        datetime CreatedAt "Audit Field"
    }
    
    SUPPLIER {
        string Id PK "Part of Composite Key"
        string TenantId PK "Part of Composite Key"
        string Name "nvarchar(500) - Hebrew"
        datetime CreatedAt "Audit Field"
    }
    
    WAYBILL {
        string Id PK "Primary Key"
        string TenantId FK "Foreign Key to Tenant"
        string ProjectId FK "Foreign Key to Project"
        string SupplierId FK "Part of Composite FK"
        date WaybillDate "Indexed"
        date DeliveryDate "Indexed"
        string ProductCode "varchar(50)"
        string ProductName "nvarchar(500) - Hebrew"
        decimal Quantity "decimal(18,3)"
        string Unit "nvarchar(20) - Hebrew"
        decimal UnitPrice "decimal(18,2)"
        decimal TotalAmount "decimal(18,2)"
        string Currency "varchar(3) - Default: ILS"
        int Status "Enum as int - Indexed"
        string VehicleNumber "varchar(20) - Nullable"
        string DriverName "nvarchar(200) - Hebrew, Nullable"
        string DeliveryAddress "nvarchar(1000) - Hebrew"
        string Notes "nvarchar(2000) - Hebrew, Nullable"
        datetime CreatedAt "Audit Field"
        datetime UpdatedAt "Audit Field - Nullable"
        byte[] Version "rowversion - Optimistic Locking"
    }
    
    Note right of SUPPLIER: Composite Primary Key (TenantId, Id)\nAllows same supplier ID across tenants\nExample: Both TENANT001 and TENANT002\ncan have supplier "SUP003"
    
    Note right of WAYBILL: Composite Foreign Key to Supplier\n(TenantId, SupplierId) references\nSupplier(TenantId, Id)\nEnsures referential integrity
```

## Key Relationships

### 1. Tenant → Projects (One-to-Many)
- **Relationship**: One tenant has many projects
- **Foreign Key**: `Project.TenantId` → `Tenant.Id`
- **Delete Behavior**: `Restrict` (cannot delete tenant with projects)
- **Isolation**: Projects are tenant-scoped, cannot be shared

### 2. Tenant → Suppliers (One-to-Many)
- **Relationship**: One tenant has many suppliers
- **Foreign Key**: `Supplier.TenantId` → `Tenant.Id`
- **Delete Behavior**: `Restrict` (cannot delete tenant with suppliers)
- **Isolation**: Suppliers use composite key `(TenantId, Id)` allowing same supplier ID across tenants

### 3. Tenant → Waybills (One-to-Many)
- **Relationship**: One tenant has many waybills
- **Foreign Key**: `Waybill.TenantId` → `Tenant.Id`
- **Delete Behavior**: `Restrict` (cannot delete tenant with waybills)
- **Isolation**: Waybills are tenant-scoped, primary isolation mechanism

### 4. Project → Waybills (One-to-Many)
- **Relationship**: One project has many waybills
- **Foreign Key**: `Waybill.ProjectId` → `Project.Id`
- **Delete Behavior**: `Restrict` (cannot delete project with waybills)
- **Isolation**: Automatically tenant-scoped through Waybill.TenantId

### 5. Supplier → Waybills (One-to-Many)
- **Relationship**: One supplier has many waybills
- **Foreign Key**: `Waybill(TenantId, SupplierId)` → `Supplier(TenantId, Id)`
- **Delete Behavior**: `Restrict` (cannot delete supplier with waybills)
- **Isolation**: Composite foreign key ensures tenant-scoped relationship

## Indexes

### Tenant Table
- **Primary Key**: `Id` (clustered index)

### Project Table
- **Primary Key**: `Id` (clustered index)
- **Index**: `TenantId` (for tenant-scoped queries)
- **Index**: `Name` (for search operations)

### Supplier Table
- **Primary Key**: `(TenantId, Id)` (composite, clustered index)
- **Index**: `TenantId` (redundant but explicit for documentation)

### Waybill Table
- **Primary Key**: `Id` (clustered index)
- **Index**: `TenantId` (for tenant-scoped queries)
- **Index**: `ProjectId` (for project filtering)
- **Index**: `SupplierId` (for supplier filtering)
- **Index**: `WaybillDate` (for date range filtering)
- **Index**: `DeliveryDate` (for delivery date filtering)
- **Index**: `Status` (for status filtering)
- **Composite Index**: `(TenantId, WaybillDate)` (for tenant-scoped date queries)
- **Composite Index**: `(TenantId, Status)` (for tenant-scoped status queries)

## Tenant Isolation Strategy

All entities except `Tenant` include a `TenantId` foreign key that references `Tenant.Id`. The `ApplicationDbContext` uses global query filters to automatically add `WHERE TenantId = @currentTenantId` to all queries, ensuring:

1. **Automatic Filtering**: All queries are automatically scoped to the current tenant
2. **Defense-in-Depth**: Even if application code forgets to filter, database enforces isolation
3. **Composite Keys**: Suppliers use `(TenantId, Id)` to allow same supplier ID across tenants while maintaining isolation
4. **Referential Integrity**: Foreign keys ensure data consistency and prevent orphaned records

## Data Types

### String Fields
- **Hebrew Text**: Uses `nvarchar` to support Unicode Hebrew characters (U+0590 to U+05FF)
- **ASCII Text**: Uses `varchar` for codes, IDs, and non-Hebrew text
- **Length Constraints**: All string fields have maximum length constraints

### Numeric Fields
- **Quantities**: `decimal(18,3)` for precise quantity tracking (supports fractional values)
- **Currency**: `decimal(18,2)` for financial amounts (2 decimal places for currency precision)

### Date Fields
- **Dates**: `date` type for waybill and delivery dates (no time component)
- **Timestamps**: `datetime` for audit fields (CreatedAt, UpdatedAt)

### Special Fields
- **Status**: `int` (enum stored as integer: 0=Pending, 1=Delivered, 2=Cancelled, 3=Disputed)
- **Version**: `rowversion` (SQL Server timestamp) for optimistic concurrency control

## Audit Fields

All entities include audit fields for tracking:
- **CreatedAt**: Timestamp when entity was created (required, auto-set)
- **UpdatedAt**: Timestamp when entity was last modified (nullable, updated on changes)
- **Version**: Row version for optimistic locking (Waybill only)

## Constraints

### Primary Keys
- **Tenant**: `Id`
- **Project**: `Id`
- **Supplier**: `(TenantId, Id)` (composite)
- **Waybill**: `Id`

### Foreign Keys
- **Project.TenantId** → `Tenant.Id` (Restrict on delete)
- **Supplier.TenantId** → `Tenant.Id` (Restrict on delete)
- **Waybill.TenantId** → `Tenant.Id` (Restrict on delete)
- **Waybill.ProjectId** → `Project.Id` (Restrict on delete)
- **Waybill(TenantId, SupplierId)** → `Supplier(TenantId, Id)` (Restrict on delete, composite)

### Check Constraints
- **Waybill.Quantity**: Business rule validation (0.5 to 50) enforced in application layer
- **Waybill.DeliveryDate**: Business rule validation (cannot be before WaybillDate) enforced in application layer
- **Waybill.Status**: Enum values enforced in application layer

## Normalization

The schema follows third normal form (3NF):
- **Projects** and **Suppliers** are separate entities (not embedded in Waybill)
- **Tenant** is top-level entity for multi-tenant isolation
- No redundant data storage
- Foreign keys maintain referential integrity

## Performance Considerations

1. **Indexes**: Strategic indexes on frequently queried fields (TenantId, Status, Dates)
2. **Composite Indexes**: Optimize tenant-scoped queries with composite indexes
3. **Query Filters**: Global query filters ensure efficient tenant-scoped queries
4. **Hebrew Text**: `nvarchar` ensures proper Unicode support without performance penalty
