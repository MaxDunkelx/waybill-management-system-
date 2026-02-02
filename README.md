# Waybill Management System

A production-quality .NET 10 Web API for managing construction waybills with multi-tenant architecture, Hebrew text support, and comprehensive business logic.

## 🎯 Assignment Completion Status

### ✅ Core Requirements (100% Complete)

- [x] **CSV Data Import & Validation** - Full UTF-8 Hebrew support, validation, upsert logic, bulk import (10,000+ records)
- [x] **Multi-Tenant Architecture** - Complete isolation via `X-Tenant-ID` header, global query filters, tenant validation
- [x] **RESTful API Endpoints** - All required endpoints implemented with filtering, pagination, and search
- [x] **Filtering & Search** - Date range, status, project, supplier, product code, Hebrew text search
- [x] **Aggregation & Analytics** - Summary statistics, monthly breakdown, top suppliers, project totals
- [x] **Concurrency Handling** - Single-user execution pattern, optimistic locking with version checking
- [x] **Message Broker Integration** - RabbitMQ event-driven architecture with `WaybillsImported` events
- [x] **Architecture Design** - Mail Uploader Service design (diagrams and documentation)
- [x] **Documentation** - Comprehensive XML docs, README, Swagger, database schema, architecture decisions

### ✅ Bonus Challenges (100% Complete)

- [x] **Bonus 1: ERP Integration Simulation** - Mock Priority ERP with retry logic, exponential backoff, sync status tracking
- [x] **Bonus 2: Background Processing** - Job queue system with async import, job status tracking
- [x] **Bonus 3: Caching Strategy** - Redis caching with fallback, cache invalidation, TTL management
- [x] **Bonus 4: Unit & Integration Tests** - xUnit tests for business logic, multi-tenant isolation, API integration

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- Docker & Docker Compose
- SQL Server (via Docker)
- RabbitMQ (via Docker)
- Redis (via Docker, optional)

### 1. Start Services

```bash
docker-compose up -d
```

This starts:
- **SQL Server** on port `1433`
- **RabbitMQ** on port `5672` (Management UI: `http://localhost:15672`, admin/admin)
- **Redis** on port `6379`

### 2. Setup Database

```bash
cd backend
dotnet ef database update
```

### 3. Run Backend API

```bash
cd backend
dotnet run
```

API available at:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `https://localhost:5001/swagger`

### 4. Run Frontend (Optional)

```bash
cd frontend
npm install
npm run dev
```

Frontend available at: `http://localhost:5173`

## 📋 System Architecture

### Multi-Tenant Isolation

The system enforces tenant isolation at multiple layers:

1. **Middleware Layer** (`TenantMiddleware`): Extracts `X-Tenant-ID` header, validates tenant exists
2. **Service Layer** (`ITenantService`): Provides current tenant context to all services
3. **Database Layer** (EF Core Global Query Filters): Automatically filters all queries by `TenantId`
4. **Entity Level**: All entities include `TenantId` as part of composite keys where needed

**Tenant Isolation Flow:**
```
HTTP Request → TenantMiddleware → TenantService → EF Core Query Filter → Database
```

### Request Flow

```
1. HTTP Request arrives with X-Tenant-ID header
2. TenantMiddleware extracts and validates tenant
3. TenantService stores tenant in HttpContext
4. Controller receives request
5. Service layer uses ITenantService to get tenant
6. EF Core automatically applies global query filter
7. Database query only returns tenant's data
8. Response returned with tenant-scoped data
```

### CSV Import Flow

```
1. POST /api/WaybillImport/import with CSV file
2. TenantMiddleware validates X-Tenant-ID header
3. WaybillImportService parses CSV with CsvHelper
4. WaybillValidationService validates each row:
   - Required fields
   - Data types
   - Business rules (quantity range, date ordering, etc.)
   - Tenant ID match (CSV tenant_id must match header)
5. For each valid row:
   - Ensure Tenant exists (create if needed)
   - Ensure Project exists (create if needed)
   - Ensure Supplier exists (create if needed)
   - Upsert Waybill (update if exists, insert if new)
6. Publish WaybillsImported event to RabbitMQ
7. Return ImportResultDto with success/error counts
```

### Event-Driven Architecture

```
1. CSV Import completes successfully
2. WaybillImportService publishes WaybillsImportedEvent to RabbitMQ
3. WaybillEventConsumer (hosted service) receives event
4. Consumer processes event (logs, updates statistics, etc.)
5. Event processing logged for audit
```

## 📁 Project Structure

```
gekko/
├── backend/                    # .NET 10 Web API
│   ├── Controllers/           # API endpoints
│   │   ├── WaybillsController.cs
│   │   ├── WaybillImportController.cs
│   │   ├── SuppliersController.cs
│   │   ├── ReportsController.cs
│   │   ├── JobsController.cs
│   │   └── MockErpController.cs
│   ├── Services/             # Business logic
│   │   ├── WaybillService.cs
│   │   ├── WaybillImportService.cs
│   │   ├── WaybillValidationService.cs
│   │   ├── TenantService.cs
│   │   ├── ErpIntegrationService.cs
│   │   ├── JobService.cs
│   │   └── RedisCacheService.cs
│   ├── Models/               # Entity models
│   │   ├── Waybill.cs
│   │   ├── Supplier.cs
│   │   ├── Project.cs
│   │   └── Tenant.cs
│   ├── Data/                 # EF Core DbContext
│   │   └── ApplicationDbContext.cs
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Middleware/           # Custom middleware
│   │   └── TenantMiddleware.cs
│   └── Migrations/           # Database migrations
├── backend.Tests/            # Unit & Integration tests
│   ├── UnitTests/
│   └── IntegrationTests/
├── frontend/                 # React/TypeScript UI
│   └── src/
├── docs/                     # Comprehensive documentation
│   ├── ARCHITECTURE.md
│   ├── ARCHITECTURE_DIAGRAMS.md
│   ├── DATABASE_SCHEMA.md
│   ├── TESTING_GUIDE.md
│   └── ...
└── docker-compose.yml        # Docker services configuration
```

## 🔌 API Endpoints

### Waybills

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/Waybills` | List waybills with filtering & pagination |
| `GET` | `/api/Waybills/{id}` | Get single waybill by ID |
| `PUT` | `/api/Waybills/{id}` | Update waybill (with optimistic locking) |
| `PATCH` | `/api/Waybills/{id}/status` | Update waybill status (with business rules) |
| `GET` | `/api/Waybills/summary` | Get aggregated statistics |
| `GET` | `/api/Waybills/projects/{projectId}/waybills` | Get waybills for a project |

### Import

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/WaybillImport/import` | Import CSV file (synchronous) |
| `POST` | `/api/WaybillImport/import-async` | Import CSV file (asynchronous, returns job ID) |

### Suppliers

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/Suppliers/{id}/summary` | Get supplier statistics |

### Reports

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/Reports/generate-monthly-report` | Generate monthly report (single-user execution) |

### Jobs

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/Jobs/{id}` | Get background job status |

### Mock ERP

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/MockErp/sync-waybill` | Simulate ERP sync (for testing) |

## 🔐 Multi-Tenant Usage

All API requests require the `X-Tenant-ID` header:

```bash
curl -H "X-Tenant-ID: TENANT001" https://localhost:5001/api/Waybills
```

**Valid Tenant IDs:**
- `TENANT001`
- `TENANT002`
- `TENANT003`

**Swagger UI:** Click the "Authorize" button (🔒) at the top right and enter a tenant ID to set it globally for all requests.

## 📊 Database Schema

### Entities

- **Tenant** - Multi-tenant isolation root
- **Project** - Construction projects (tenant-scoped)
- **Supplier** - Concrete suppliers (composite key: `TenantId`, `Id`)
- **Waybill** - Delivery waybills (tenant-scoped, references Supplier via composite FK)
- **Job** - Background job tracking

### Key Design Decisions

1. **Composite Keys**: `Supplier` uses `(TenantId, Id)` to allow same supplier ID across tenants
2. **Global Query Filters**: All queries automatically filtered by `TenantId`
3. **Optimistic Locking**: `Waybill.Version` (rowversion) prevents concurrent update conflicts
4. **Hebrew Support**: All text columns use `nvarchar` (Unicode)

See `docs/DATABASE_SCHEMA.md` for complete schema documentation.

## 🧪 Testing

### Unit Tests

```bash
cd backend.Tests
dotnet test
```

Tests cover:
- Business logic validation
- Status transition rules
- Filtering and pagination
- Multi-tenant isolation

### Integration Tests

```bash
cd backend.Tests
dotnet test --filter Category=Integration
```

Tests verify:
- Multi-tenant data isolation
- CSV import with validation
- API endpoint behavior

### Manual Testing

See `docs/TESTING_GUIDE.md` for comprehensive manual testing instructions with curl commands.

## 🎁 Bonus Features

### 1. ERP Integration Simulation

- Mock ERP endpoint with 10% random failure rate
- Exponential backoff retry logic
- Background service syncs `PendingSync` waybills
- Tracks sync status: `PENDING_SYNC`, `SYNCED`, `SYNC_FAILED`

### 2. Background Processing

- Async CSV import returns job ID immediately
- Job queue processes imports in background
- `GET /api/Jobs/{id}` to check job status
- In-memory queue (upgradeable to Redis/Hangfire)

### 3. Caching Strategy

- Redis caching for summary statistics
- Memory cache fallback if Redis unavailable
- Cache invalidation on data updates
- TTL-based expiration

### 4. Unit & Integration Tests

- xUnit test framework
- Moq for mocking
- Tests for business logic, validation, multi-tenant isolation
- Integration tests for API endpoints

## 📚 Documentation

Comprehensive documentation available in `docs/`:

- **ARCHITECTURE.md** - System architecture overview
- **ARCHITECTURE_DIAGRAMS.md** - Mermaid flow diagrams
- **DATABASE_SCHEMA.md** - Complete database schema
- **ERD.md** - Entity Relationship Diagram
- **TESTING_GUIDE.md** - Step-by-step testing instructions
- **TESTING_CHECKLIST.md** - Testing checklist
- **QUICK_START.md** - 5-minute setup guide
- **MAIL_UPLOADER_ARCHITECTURE.md** - Architecture design challenge
- **CACHING_STRATEGY.md** - Caching implementation details
- **TENANT_ISOLATION_ENFORCEMENT.md** - Multi-tenant isolation details

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=WaybillManagementDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "RabbitMQ": {
    "ConnectionString": "amqp://admin:admin@localhost:5672/",
    "QueueName": "waybills_imported"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "WaybillManagement"
  }
}
```

## 🐛 Troubleshooting

### SQL Server Connection Issues

```bash
# Check if container is running
docker ps | grep sqlserver

# Check logs
docker logs gekko-sqlserver

# Restart container
docker restart gekko-sqlserver
```

### RabbitMQ Connection Issues

```bash
# Check if container is running
docker ps | grep rabbitmq

# Access management UI
open http://localhost:15672
# Login: admin/admin
```

### Redis Connection Issues

```bash
# Check if container is running
docker ps | grep redis

# Test connection
docker exec -it gekko-redis redis-cli ping
```

### Hebrew Text Display Issues

1. Verify UTF-8 encoding in `Program.cs`
2. Check database columns are `nvarchar` (not `varchar`)
3. Ensure HTTP response includes `charset=utf-8`

## 🏗️ Architecture Decisions

### Why Composite Keys for Suppliers?

Different tenants can have suppliers with the same ID (e.g., both TENANT001 and TENANT002 can have SUP001). Using a composite key `(TenantId, Id)` ensures:
- Data isolation maintained
- No conflicts between tenants
- Foreign keys work correctly

### Why Global Query Filters?

EF Core global query filters automatically add `WHERE TenantId = @tenantId` to all queries, preventing accidental cross-tenant data access at the database level.

### Why Optimistic Locking?

Using `rowversion` (byte array) for optimistic locking prevents lost updates when multiple users edit the same waybill concurrently. The `Version` field is automatically updated by SQL Server on each update.

### Why Event-Driven Architecture?

Publishing `WaybillsImported` events allows:
- Decoupled processing (notifications, statistics, audit logs)
- Scalability (multiple consumers can process events)
- Resilience (events can be retried if processing fails)

## 📝 Assignment Compliance

See `docs/ASSIGNMENT_COMPLIANCE_ANALYSIS.md` for detailed verification against all assignment requirements.

## 🚢 Deployment

### Production Considerations

1. **Security**: Replace `X-Tenant-ID` header with JWT claims
2. **Database**: Use connection pooling, read replicas for scaling
3. **Caching**: Use Redis cluster for high availability
4. **Message Broker**: Use RabbitMQ cluster or Azure Service Bus
5. **Monitoring**: Add Application Insights or similar
6. **Logging**: Use structured logging (Serilog, NLog)

## 📄 License

This project is part of a technical assessment for Gekko Technology Solutions.

## 👤 Author

Built as part of the Backend Developer At-Home Technical Assignment.

---

**For detailed setup and testing instructions, see `docs/QUICK_START.md` and `docs/TESTING_GUIDE.md`.**
