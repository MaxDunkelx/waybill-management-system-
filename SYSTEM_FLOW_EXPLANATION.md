# System Flow Explanation - Complete Request-to-Response Flow

## 🚀 Servers Running

**Backend**: `http://localhost:5001`  
**Frontend**: `http://localhost:5173`  
**Swagger**: `http://localhost:5001/swagger`

---

## 📋 Complete Request Flow - Step by Step with Code

### Example: GET /api/Waybills (List Waybills)

#### **Step 1: Frontend Request** (`frontend/src/services/api.ts`)

```typescript
// User clicks "View Waybills" in frontend
// Frontend service makes API call
const response = await apiClient.get(`/api/Waybills?pageNumber=1&pageSize=20`);
```

**What happens:**
1. `apiClient` is an Axios instance configured with base URL `http://localhost:5001`
2. **Request Interceptor** (`api.ts:19-30`):
   ```typescript
   apiClient.interceptors.request.use((config) => {
     const tenantId = getTenantId(); // Gets from localStorage
     if (tenantId) {
       config.headers['X-Tenant-ID'] = tenantId; // Adds header automatically
     }
     return config;
   });
   ```
3. Request sent: `GET http://localhost:5001/api/Waybills?pageNumber=1&pageSize=20`
4. Headers include: `X-Tenant-ID: TENANT001`

---

#### **Step 2: Backend Receives Request** (`backend/Program.cs`)

**Middleware Pipeline Order** (configured in `Program.cs:650-680`):

```csharp
app.UseCors(); // 1. CORS first (allows cross-origin requests)
app.UseMiddleware<TenantMiddleware>(); // 2. Tenant validation
app.UseAuthentication(); // 3. Authentication (if needed)
app.UseAuthorization(); // 4. Authorization (if needed)
app.MapControllers(); // 5. Route to controllers
```

**Why this order matters:**
- CORS must be first to handle preflight OPTIONS requests
- TenantMiddleware must be early to validate tenant before controllers
- Controllers are last to handle the actual request

---

#### **Step 3: Tenant Middleware** (`backend/Middleware/TenantMiddleware.cs`)

**Code Flow** (`TenantMiddleware.cs:98-196`):

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // 1. Skip validation for Swagger/health endpoints
    if (path.StartsWith("/swagger") || path == "/") {
        await _next(context);
        return;
    }
    
    // 2. Extract tenant ID from header
    if (!context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader)) {
        // Return 400 if missing
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new {
            error = "Missing tenant identifier",
            message = "The 'X-Tenant-ID' header is required"
        });
        return;
    }
    
    // 3. Store tenant ID in HttpContext.Items
    var tenantId = tenantIdHeader.ToString().Trim();
    context.Items["CurrentTenantId"] = tenantId; // KEY STEP: Makes tenant available to all services
    
    // 4. Continue to next middleware
    await _next(context);
}
```

**What happens:**
- ✅ Extracts `X-Tenant-ID: TENANT001` from request header
- ✅ Validates tenant ID is present and not empty
- ✅ Stores in `HttpContext.Items["CurrentTenantId"]` for downstream use
- ✅ If missing, returns 400 Bad Request immediately (request never reaches controller)

**Security:** This ensures NO request can proceed without a valid tenant ID.

---

#### **Step 4: Controller Receives Request** (`backend/Controllers/WaybillsController.cs`)

**Code Flow** (`WaybillsController.cs:82-99`):

```csharp
[HttpGet]
public async Task<ActionResult<PagedResultDto<WaybillListDto>>> GetWaybills([FromQuery] WaybillQueryDto query)
{
    // 1. Get tenant ID from TenantService (reads from HttpContext.Items)
    var tenantId = _tenantService.GetCurrentTenantId(); // Returns "TENANT001"
    
    // 2. Log the request
    _logger.LogInformation("Retrieving waybills for tenant {TenantId}", tenantId);
    
    // 3. Call service layer
    var result = await _waybillService.GetAllAsync(tenantId, query);
    
    // 4. Return result
    return Ok(result);
}
```

**What happens:**
- Controller receives request with query parameters (`pageNumber=1`, `pageSize=20`)
- Gets tenant ID from `ITenantService` (which reads from `HttpContext.Items`)
- Passes tenant ID and query to service layer
- Returns service result as HTTP 200 OK

---

#### **Step 5: Service Layer** (`backend/Services/WaybillService.cs`)

**Code Flow** (simplified):

```csharp
public async Task<PagedResultDto<WaybillListDto>> GetAllAsync(string tenantId, WaybillQueryDto query)
{
    // 1. Build query with filters
    var waybillsQuery = _context.Waybills
        .Where(w => w.Status == query.Status) // If status filter provided
        .Where(w => w.WaybillDate >= query.DateFrom) // If date filter provided
        .OrderByDescending(w => w.WaybillDate);
    
    // 2. Get total count (for pagination)
    var totalCount = await waybillsQuery.CountAsync();
    
    // 3. Apply pagination
    var waybills = await waybillsQuery
        .Skip((query.PageNumber - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(w => new WaybillListDto { ... }) // Map to DTO
        .ToListAsync();
    
    // 4. Return paginated result
    return new PagedResultDto<WaybillListDto> {
        Items = waybills,
        TotalCount = totalCount,
        PageNumber = query.PageNumber,
        PageSize = query.PageSize
    };
}
```

**What happens:**
- Service receives tenant ID and query parameters
- Builds LINQ query with filters
- **CRITICAL:** When EF Core executes the query, it automatically adds `WHERE TenantId = @tenantId` due to global query filter
- Applies pagination
- Maps entities to DTOs
- Returns result

---

#### **Step 6: Database Query with Global Filter** (`backend/Data/ApplicationDbContext.cs`)

**Global Query Filter** (`ApplicationDbContext.cs:141-142`):

```csharp
modelBuilder.Entity<Waybill>()
    .HasQueryFilter(w => w.TenantId == GetCurrentTenantId());
```

**What `GetCurrentTenantId()` does** (`ApplicationDbContext.cs:450-460`):

```csharp
private string GetCurrentTenantId()
{
    // Get tenant ID from TenantService (which reads from HttpContext.Items)
    if (_tenantService == null)
        throw new InvalidOperationException("TenantService is required");
    
    return _tenantService.GetCurrentTenantId(); // Returns "TENANT001"
}
```

**SQL Generated by EF Core:**

```sql
-- What EF Core actually executes:
SELECT w.*
FROM Waybills w
WHERE w.TenantId = @tenantId  -- Automatically added by global filter
  AND w.Status = @status       -- From query filter
  AND w.WaybillDate >= @dateFrom -- From query filter
ORDER BY w.WaybillDate DESC
OFFSET @skip ROWS
FETCH NEXT @take ROWS ONLY
```

**Security:** Even if developer forgets to filter by tenant, the global filter ensures only current tenant's data is returned.

---

#### **Step 7: Response Returns to Frontend**

**Backend Response:**
```json
{
  "items": [
    {
      "id": "WB-2024-001",
      "waybillDate": "2024-09-01",
      "projectName": "מגדלי הים התיכון",
      "supplierName": "בטון מודי בע\"מ",
      "status": "DELIVERED",
      "totalAmount": 5625.00
    }
  ],
  "totalCount": 27,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 2
}
```

**Frontend Receives** (`api.ts:33-61`):
```typescript
// Response interceptor handles errors
apiClient.interceptors.response.use(
  (response) => response, // Success: return response
  (error) => {
    // Error: log and handle
    if (error.response?.status === 400 && error.response?.data?.message?.includes('X-Tenant-ID')) {
      // Tenant ID missing - redirect to tenant selection
      localStorage.removeItem('tenantId');
      window.location.href = '/tenant-select';
    }
    return Promise.reject(error);
  }
);
```

**Frontend Displays:**
- Waybills list with Hebrew text
- Pagination controls
- All data automatically filtered by tenant

---

## 📋 CSV Import Flow - Complete Example

### Example: POST /api/waybills/import

#### **Step 1: Frontend Upload** (`frontend/src/components/WaybillImport.tsx`)

```typescript
const formData = new FormData();
formData.append('file', csvFile);

const response = await importService.importWaybills(formData);
```

**Request:**
- Method: `POST`
- URL: `http://localhost:5001/api/waybills/import`
- Headers: `X-Tenant-ID: TENANT001`, `Content-Type: multipart/form-data`
- Body: CSV file

---

#### **Step 2: Tenant Middleware** (Same as above)
- Validates `X-Tenant-ID: TENANT001`
- Stores in `HttpContext.Items`

---

#### **Step 3: Controller** (`backend/Controllers/WaybillImportController.cs`)

```csharp
[HttpPost("import")]
public async Task<ActionResult<ImportResultDto>> Import([FromForm] IFormFile file)
{
    var tenantId = _tenantService.GetCurrentTenantId(); // "TENANT001"
    
    // Read CSV file
    using var stream = file.OpenReadStream();
    var result = await _importService.ImportFromCsvAsync(stream, tenantId);
    
    return Ok(result);
}
```

---

#### **Step 4: Import Service** (`backend/Services/WaybillImportService.cs`)

**Flow** (simplified):

```csharp
public async Task<ImportResultDto> ImportFromCsvAsync(Stream csvStream, string tenantId)
{
    // 1. Parse CSV with CsvHelper
    using var reader = new StreamReader(csvStream, Encoding.UTF8);
    using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
        Encoding = Encoding.UTF8, // Critical for Hebrew text
        HasHeaderRecord = true,
        Delimiter = DetectDelimiter(csvStream) // Auto-detect comma or tab
    });
    
    var waybills = new List<ImportWaybillDto>();
    var errors = new List<ImportErrorDto>();
    
    // 2. Read each row
    while (await csv.ReadAsync()) {
        var waybill = csv.GetRecord<ImportWaybillDto>();
        
        // 3. Validate tenant ID match
        if (waybill.TenantId != tenantId) {
            errors.Add(new ImportErrorDto {
                RowNumber = csv.Context.Parser.Row,
                Field = "tenant_id",
                Message = $"Tenant ID mismatch: CSV contains '{waybill.TenantId}' but header specifies '{tenantId}'"
            });
            continue;
        }
        
        // 4. Validate business rules
        var validationResult = await _validationService.ValidateAsync(waybill, tenantId);
        if (!validationResult.IsValid) {
            errors.AddRange(validationResult.Errors);
            continue;
        }
        
        waybills.Add(waybill);
    }
    
    // 5. Save to database (with transaction)
    var saveResult = await SaveWaybillsToDatabaseAsync(waybills, tenantId);
    
    // 6. Publish event to RabbitMQ
    if (saveResult.SuccessCount > 0) {
        await _messagePublisher.PublishAsync(new WaybillsImportedEvent {
            TenantId = tenantId,
            WaybillCount = saveResult.SuccessCount,
            ImportedAt = DateTime.UtcNow
        });
    }
    
    // 7. Invalidate cache
    await _cacheService.RemoveByPatternAsync($"waybill:summary:{tenantId}:*");
    
    return new ImportResultDto {
        TotalRows = waybills.Count + errors.Count,
        SuccessCount = saveResult.SuccessCount,
        ErrorCount = errors.Count,
        Errors = errors
    };
}
```

**Key Steps:**
1. **Parse CSV** with UTF-8 encoding (Hebrew support)
2. **Validate tenant ID** matches header (security)
3. **Validate business rules** (quantity, dates, etc.)
4. **Save to database** (with transaction for atomicity)
5. **Publish event** to RabbitMQ (event-driven)
6. **Invalidate cache** (fresh data)

---

#### **Step 5: Database Save** (`WaybillImportService.cs:SaveWaybillsToDatabaseAsync`)

```csharp
private async Task<SaveResult> SaveWaybillsToDatabaseAsync(
    List<ImportWaybillDto> waybills, 
    string tenantId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try {
        foreach (var dto in waybills) {
            // 1. Ensure Tenant exists (create if needed)
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) {
                tenant = new Tenant { Id = tenantId, Name = $"Tenant {tenantId}" };
                _context.Tenants.Add(tenant);
            }
            
            // 2. Ensure Project exists (create if needed)
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && p.TenantId == tenantId);
            if (project == null) {
                project = new Project {
                    Id = dto.ProjectId,
                    TenantId = tenantId,
                    Name = dto.ProjectName
                };
                _context.Projects.Add(project);
            }
            
            // 3. Ensure Supplier exists (create if needed)
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.TenantId == tenantId);
            if (supplier == null) {
                supplier = new Supplier {
                    Id = dto.SupplierId,
                    TenantId = tenantId,
                    Name = dto.SupplierName
                };
                _context.Suppliers.Add(supplier);
            }
            
            // 4. Upsert Waybill (update if exists, insert if new)
            var existingWaybill = await _context.Waybills
                .FirstOrDefaultAsync(w => w.Id == dto.WaybillId && w.TenantId == tenantId);
            
            if (existingWaybill != null) {
                // Update existing
                existingWaybill.Quantity = dto.Quantity;
                existingWaybill.Status = dto.Status;
                // ... update other fields
            } else {
                // Insert new
                var waybill = new Waybill {
                    Id = dto.WaybillId,
                    TenantId = tenantId,
                    ProjectId = dto.ProjectId,
                    SupplierId = dto.SupplierId,
                    Quantity = dto.Quantity,
                    Status = dto.Status,
                    // ... other fields
                };
                _context.Waybills.Add(waybill);
            }
        }
        
        // 5. Save all changes
        await _context.SaveChangesAsync();
        
        // 6. Commit transaction
        await transaction.CommitAsync();
        
        return new SaveResult { SuccessCount = waybills.Count };
    } catch (Exception ex) {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Key Points:**
- **Transaction:** All-or-nothing (if one fails, all rollback)
- **Upsert Logic:** Updates existing waybills, inserts new ones
- **Auto-Create:** Creates Tenant/Project/Supplier if they don't exist
- **Tenant Isolation:** All queries filtered by `TenantId` automatically

---

#### **Step 6: Event Publishing** (`backend/Services/MessagePublisher.cs`)

```csharp
public async Task PublishAsync<T>(T message) where T : class
{
    var routingKey = typeof(T).Name; // "WaybillsImportedEvent"
    var body = JsonSerializer.SerializeToUtf8Bytes(message);
    
    _channel.BasicPublish(
        exchange: "waybills_exchange",
        routingKey: routingKey,
        basicProperties: null,
        body: body
    );
}
```

**What happens:**
- Message published to RabbitMQ exchange
- Routing key: `WaybillsImportedEvent`
- Body: JSON serialized event

---

#### **Step 7: Event Consumer** (`backend/Services/MessageConsumer.cs`)

**Background Service** (runs continuously):

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var consumer = new EventingBasicConsumer(_channel);
    consumer.Received += async (model, ea) => {
        var body = ea.Body.ToArray();
        var message = JsonSerializer.Deserialize<WaybillsImportedEvent>(body);
        
        // Process event (log, update statistics, etc.)
        _logger.LogInformation(
            "Processing waybill import event for tenant {TenantId}, " +
            "imported {WaybillCount} waybills",
            message.TenantId,
            message.WaybillCount);
        
        // Acknowledge message
        _channel.BasicAck(ea.DeliveryTag, false);
    };
    
    _channel.BasicConsume("waybills_imported_queue", false, consumer);
}
```

**What happens:**
- Background service listens to RabbitMQ queue
- Receives event when waybills imported
- Processes event (logs, updates statistics)
- Acknowledges message (removes from queue)

---

## 🔍 How to Check Everything

### 1. Check Backend is Running

```bash
curl http://localhost:5001/swagger/index.html
# Should return HTML (not empty)
```

### 2. Check Frontend is Running

```bash
curl http://localhost:5173
# Should return HTML with Vite client script
```

### 3. Test Tenant Middleware

```bash
# Without tenant ID (should fail)
curl http://localhost:5001/api/Waybills
# Response: 400 Bad Request with error message

# With tenant ID (should succeed)
curl -H "X-Tenant-ID: TENANT001" http://localhost:5001/api/Waybills
# Response: 200 OK with waybills data
```

### 4. Test Database Isolation

```bash
# As TENANT001
curl -H "X-Tenant-ID: TENANT001" http://localhost:5001/api/Waybills

# As TENANT002 (should return different data)
curl -H "X-Tenant-ID: TENANT002" http://localhost:5001/api/Waybills

# Verify: Results are different (tenant isolation working)
```

### 5. Test CSV Import

1. Open frontend: `http://localhost:5173`
2. Select tenant: TENANT001
3. Navigate to: `/import`
4. Upload CSV file
5. Check response: Should show success/error counts

### 6. Check RabbitMQ Events

1. Open RabbitMQ Management: `http://localhost:15672` (admin/admin)
2. Navigate to: Queues → `waybills_imported_queue`
3. Check: Messages should appear after import

### 7. Check Cache

```bash
# Connect to Redis
docker exec -it gekko-redis redis-cli

# List cache keys
KEYS waybill:summary:TENANT001:*

# Get cached value
GET waybill:summary:TENANT001:all
```

---

## 🔄 Complete Request Flow Diagram

```
┌─────────────┐
│   Frontend  │
│  (Browser)  │
└──────┬──────┘
       │ 1. HTTP Request + X-Tenant-ID header
       ▼
┌─────────────────────────────────────┐
│   Backend (ASP.NET Core)             │
│                                       │
│  ┌──────────────────────────────┐  │
│  │ 1. CORS Middleware             │  │  ← Allows cross-origin requests
│  └──────────────┬─────────────────┘  │
│                 │                     │
│  ┌──────────────▼─────────────────┐  │
│  │ 2. TenantMiddleware             │  │  ← Extracts X-Tenant-ID, stores in HttpContext.Items
│  └──────────────┬─────────────────┘  │
│                 │                     │
│  ┌──────────────▼─────────────────┐  │
│  │ 3. Controller                   │  │  ← Receives request, gets tenant from TenantService
│  │    (WaybillsController)         │  │
│  └──────────────┬─────────────────┘  │
│                 │                     │
│  ┌──────────────▼─────────────────┐  │
│  │ 4. Service Layer               │  │  ← Business logic, builds query
│  │    (WaybillService)            │  │
│  └──────────────┬─────────────────┘  │
│                 │                     │
│  ┌──────────────▼─────────────────┐  │
│  │ 5. ApplicationDbContext         │  │  ← EF Core DbContext
│  │    (with Global Query Filter)   │  │  ← Automatically adds WHERE TenantId = @tenantId
│  └──────────────┬─────────────────┘  │
└─────────────────┼─────────────────────┘
                  │ 6. SQL Query
                  ▼
         ┌────────────────┐
         │  SQL Server    │
         │   Database     │
         └────────┬───────┘
                  │ 7. Results (filtered by TenantId)
                  ▼
         ┌────────────────┐
         │  Application   │
         │   DbContext    │
         └────────┬───────┘
                  │ 8. Entities → DTOs
                  ▼
         ┌────────────────┐
         │  Service       │
         │   Layer        │
         └────────┬───────┘
                  │ 9. DTOs
                  ▼
         ┌────────────────┐
         │  Controller    │
         │  (returns 200) │
         └────────┬───────┘
                  │ 10. JSON Response
                  ▼
         ┌────────────────┐
         │   Frontend     │
         │  (displays)    │
         └────────────────┘
```

---

## 🔐 Security Flow - Multi-Tenant Isolation

### Layer 1: Middleware (`TenantMiddleware.cs`)

```csharp
// Validates tenant ID exists in request
if (!context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader)) {
    return 400 Bad Request; // Request rejected
}
context.Items["CurrentTenantId"] = tenantId; // Stored for downstream use
```

**Security:** No request can proceed without tenant ID.

---

### Layer 2: Service Layer (`ITenantService`)

```csharp
public string GetCurrentTenantId() {
    // Reads from HttpContext.Items (set by middleware)
    return _httpContextAccessor.HttpContext.Items["CurrentTenantId"];
}
```

**Security:** Provides tenant context to all services.

---

### Layer 3: Database Layer (`ApplicationDbContext`)

```csharp
modelBuilder.Entity<Waybill>()
    .HasQueryFilter(w => w.TenantId == GetCurrentTenantId());
```

**Security:** All queries automatically filtered by tenant. Even if developer forgets to filter, database enforces isolation.

---

### Layer 4: Entity Level (Composite Keys)

```csharp
// Supplier uses composite key (TenantId, Id)
modelBuilder.Entity<Supplier>()
    .HasKey(s => new { s.TenantId, s.Id });
```

**Security:** Prevents different tenants from having suppliers with same ID.

---

## ✅ Verification Checklist

### Core Functionality

- [ ] **Backend Running**: `curl http://localhost:5001/swagger` returns HTML
- [ ] **Frontend Running**: `curl http://localhost:5173` returns HTML
- [ ] **Tenant Middleware**: Request without `X-Tenant-ID` returns 400
- [ ] **Tenant Isolation**: Different tenants see different data
- [ ] **CSV Import**: Upload CSV, verify import results
- [ ] **Filtering**: Test status, date, search filters
- [ ] **Pagination**: Test page navigation
- [ ] **Status Update**: Test status transitions
- [ ] **Summary**: Verify statistics display correctly
- [ ] **RabbitMQ**: Check events published after import
- [ ] **Cache**: Verify faster responses after first load

### Data Integrity

- [ ] **Hebrew Text**: Displays correctly throughout
- [ ] **Tenant Isolation**: No cross-tenant data visible
- [ ] **Business Rules**: Status transitions enforced
- [ ] **Validation**: Invalid data rejected with clear errors

---

## 📚 Key Files Reference

### Backend

- **`Program.cs`**: Middleware pipeline, service registration
- **`Middleware/TenantMiddleware.cs`**: Tenant validation
- **`Services/TenantService.cs`**: Tenant context provider
- **`Data/ApplicationDbContext.cs`**: Global query filters
- **`Controllers/WaybillsController.cs`**: API endpoints
- **`Services/WaybillService.cs`**: Business logic
- **`Services/WaybillImportService.cs`**: CSV import logic

### Frontend

- **`src/services/api.ts`**: API client with tenant header injection
- **`src/services/waybillService.ts`**: Waybill API calls
- **`src/components/WaybillList.tsx`**: Waybill list UI
- **`src/components/WaybillImport.tsx`**: CSV import UI

---

**System is ready for testing and submission!**
