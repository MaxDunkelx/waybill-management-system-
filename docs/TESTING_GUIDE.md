# Waybill Management System - Comprehensive Testing Guide

This guide provides step-by-step instructions for manually testing all features of the Waybill Management System.

---

## Table of Contents

1. [Prerequisites & Setup](#section-1-prerequisites--setup)
2. [Test Data Setup](#section-2-test-data-setup)
3. [Multi-Tenant Isolation Testing](#section-3-multi-tenant-isolation-testing-critical)
4. [CSV Import Testing](#section-4-csv-import-testing)
5. [API Endpoint Testing](#section-5-api-endpoint-testing)
6. [Concurrency Testing](#section-6-concurrency-testing)
7. [Message Broker Testing](#section-7-message-broker-testing)
8. [Error Handling Testing](#section-8-error-handling-testing)
9. [Performance Testing](#section-9-performance-testing)

---

## Section 1: Prerequisites & Setup

### 1.1 Required Tools

- **Docker Desktop** (for running SQL Server, RabbitMQ, Redis)
- **.NET 10.0 SDK** (for running the API)
- **Postman** or **curl** (for API testing)
- **SQL Server Management Studio** or **Azure Data Studio** (optional, for database inspection)
- **Web Browser** (for Swagger UI)

### 1.2 Start Docker Services

```bash
# Navigate to project root
cd /Users/maxdunkel/gekko

# Start all services (SQL Server, RabbitMQ, Redis)
docker-compose up -d

# Verify services are running
docker-compose ps
```

**Expected Output:**
```
NAME                STATUS
gekko-sqlserver     Up
gekko-rabbitmq      Up
gekko-redis         Up
```

**Verify Services:**
- SQL Server: `docker exec -it gekko-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"`
- RabbitMQ: Open http://localhost:15672 (admin/admin)
- Redis: `docker exec -it gekko-redis redis-cli ping` (should return "PONG")

### 1.3 Run Database Migrations

```bash
# Navigate to backend directory
cd backend

# Apply migrations
dotnet ef database update --project .

# Verify migration
dotnet ef migrations list --project .
```

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20260201084155_InitialCreate'.
Done.
```

### 1.4 Start the API

```bash
# In backend directory
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

**Verify API:**
- Open http://localhost:5000 (Swagger UI)
- Should see API documentation

### 1.5 Verify Services Are Running

**Check API Health:**
```bash
curl http://localhost:5000
```

**Check Swagger:**
- Open http://localhost:5000 in browser
- Should see Swagger UI with all endpoints

---

## Section 2: Test Data Setup

### 2.1 Create Test Tenants

**Option 1: Using SQL (Recommended)**

```sql
-- Connect to SQL Server
-- Server: localhost,1433
-- User: sa
-- Password: YourStrong@Passw0rd
-- Database: WaybillManagementDB

USE WaybillManagementDB;

-- Create test tenants
INSERT INTO Tenants (Id, Name, CreatedAt) VALUES
('TENANT001', 'Test Tenant 1', GETUTCDATE()),
('TENANT002', 'Test Tenant 2', GETUTCDATE()),
('TENANT003', 'Test Tenant 3', GETUTCDATE());

-- Verify tenants
SELECT * FROM Tenants;
```

**Option 2: Using API (if tenant creation endpoint exists)**

```bash
# Note: Currently no tenant creation endpoint exists
# Use SQL method above
```

### 2.2 Verify Tenants Exist

```bash
# Test tenant endpoint (if exists)
curl -H "X-Tenant-ID: TENANT001" http://localhost:5000/api/tenant/test
```

**Expected Response:**
```json
{
  "tenantId": "TENANT001",
  "message": "Tenant middleware is working correctly"
}
```

---

## Section 3: Multi-Tenant Isolation Testing (CRITICAL)

### Test 3.1: Tenant Header Required

**Test:** Make request without X-Tenant-ID header

```bash
curl -X GET http://localhost:5000/api/waybills
```

**Expected Response:**
- **Status Code:** 400 Bad Request
- **Response Body:**
```json
{
  "error": "Missing tenant identifier",
  "message": "The 'X-Tenant-ID' header is required for all requests.",
  "headerName": "X-Tenant-ID"
}
```

**✅ PASS Criteria:**
- Returns 400 Bad Request
- Clear error message
- No data returned

---

### Test 3.2: Tenant Isolation - Waybills

**Test Steps:**

1. **Import CSV with TENANT001:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-waybills.csv" \
  http://localhost:5000/api/waybills/import
```

2. **Query waybills with TENANT001:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills"
```

**Expected:** Should see imported waybills

3. **Query waybills with TENANT002:**
```bash
curl -H "X-Tenant-ID: TENANT002" \
  "http://localhost:5000/api/waybills"
```

**Expected:** Should see NO waybills (empty array)

4. **Try to get waybill by ID with wrong tenant:**
```bash
# First, get a waybill ID from TENANT001
WAYBILL_ID="WB-2024-001"  # Replace with actual ID

# Try to access with TENANT002
curl -H "X-Tenant-ID: TENANT002" \
  "http://localhost:5000/api/waybills/$WAYBILL_ID"
```

**Expected Response:**
- **Status Code:** 404 Not Found
- **Response Body:**
```json
{
  "error": "Waybill not found",
  "message": "Waybill with ID 'WB-2024-001' was not found or does not belong to your tenant."
}
```

**✅ PASS Criteria:**
- TENANT001 sees its data
- TENANT002 sees NO data
- Cross-tenant access returns 404

---

### Test 3.3: Tenant Isolation - Projects

**Test Steps:**

1. **Query projects with TENANT001:**
```bash
# Get waybills for a project (projects are created during import)
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/projects/PRJ001/waybills"
```

**Expected:** Should see waybills for project PRJ001

2. **Query same project ID with TENANT002:**
```bash
curl -H "X-Tenant-ID: TENANT002" \
  "http://localhost:5000/api/projects/PRJ001/waybills"
```

**Expected:** Should see empty array or 404

**✅ PASS Criteria:**
- Projects are tenant-isolated
- Cross-tenant project access returns empty/404

---

### Test 3.4: Tenant Isolation - Suppliers

**Test Steps:**

1. **Query supplier summary with TENANT001:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/suppliers/SUP001/summary"
```

**Expected:** Should see supplier summary

2. **Query same supplier ID with TENANT002:**
```bash
curl -H "X-Tenant-ID: TENANT002" \
  "http://localhost:5000/api/suppliers/SUP001/summary"
```

**Expected Response:**
- **Status Code:** 404 Not Found
- **Response Body:**
```json
{
  "error": "Supplier not found",
  "message": "Supplier with ID 'SUP001' was not found or does not belong to your tenant."
}
```

**✅ PASS Criteria:**
- Suppliers are tenant-isolated
- Cross-tenant supplier access returns 404

---

### Test 3.5: Tenant Isolation - Summary

**Test Steps:**

1. **Get summary with TENANT001:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/summary"
```

**Expected:** Should see summary with TENANT001 data only

2. **Get summary with TENANT002:**
```bash
curl -H "X-Tenant-ID: TENANT002" \
  "http://localhost:5000/api/waybills/summary"
```

**Expected:** Should see summary with TENANT002 data only (different totals, possibly zeros)

**✅ PASS Criteria:**
- Summaries are tenant-scoped
- Different tenants see different totals

---

### Test 3.6: Cross-Tenant Data Access Prevention

**Test Steps:**

1. **Get waybill ID from TENANT001:**
```bash
WAYBILL_ID="WB-2024-001"  # Replace with actual ID
```

2. **Try to update waybill from TENANT001 using TENANT002 header:**
```bash
curl -X PUT \
  -H "X-Tenant-ID: TENANT002" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "base64-encoded-version",
    "waybillDate": "2024-01-15",
    "deliveryDate": "2024-01-20",
    "projectId": "PRJ001",
    "supplierId": "SUP001",
    "productCode": "B30",
    "productName": "בטון ב-30",
    "quantity": 10.5,
    "unit": "מ\"ק",
    "unitPrice": 150.75,
    "totalAmount": 1582.87,
    "currency": "ILS",
    "status": "Delivered"
  }' \
  "http://localhost:5000/api/waybills/$WAYBILL_ID"
```

**Expected Response:**
- **Status Code:** 404 Not Found
- **Response Body:**
```json
{
  "error": "Waybill not found",
  "message": "Waybill with ID 'WB-2024-001' was not found or does not belong to your tenant."
}
```

**✅ PASS Criteria:**
- Cross-tenant updates return 404
- No data leakage

---

## Section 4: CSV Import Testing

### Test 4.1: Successful Import

**Create Test CSV File (`test-waybills.csv`):**

```csv
waybill_id,waybill_date,delivery_date,project_id,supplier_id,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status,vehicle_number,driver_name,delivery_address,notes
WB-2024-001,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,1582.87,ILS,Delivered,123-45-678,יוסי כהן,רחוב הרצל 1 תל אביב,Test note
WB-2024-002,2024-01-16,2024-01-21,PRJ001,SUP002,B25,בטון ב-25,5.0,מ"ק,140.50,702.50,ILS,Pending,456-78-901,דני לוי,רחוב דיזנגוף 10 תל אביב,
```

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-waybills.csv" \
  http://localhost:5000/api/waybills/import
```

**Expected Response:**
```json
{
  "totalRows": 2,
  "successCount": 2,
  "errorCount": 0,
  "errors": [],
  "warnings": [],
  "parsedWaybills": [...]
}
```

**Verify in Database:**
```sql
SELECT * FROM Waybills WHERE TenantId = 'TENANT001';
```

**✅ PASS Criteria:**
- Success count matches valid rows
- No errors
- Data appears in database
- Hebrew text stored correctly

---

### Test 4.2: Validation - Required Fields

**Create CSV with Missing waybill_id (`test-missing-id.csv`):**

```csv
waybill_id,waybill_date,delivery_date,project_id,supplier_id,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status
,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,1582.87,ILS,Delivered
```

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-missing-id.csv" \
  http://localhost:5000/api/waybills/import
```

**Expected Response:**
```json
{
  "totalRows": 1,
  "successCount": 0,
  "errorCount": 1,
  "errors": [
    {
      "rowNumber": 2,
      "field": "waybill_id",
      "message": "Waybill ID is required and cannot be empty.",
      "rowData": "..."
    }
  ]
}
```

**✅ PASS Criteria:**
- Error reported for missing field
- Row not imported
- Clear error message

---

### Test 4.3: Validation - Business Rules

**Create CSV with Business Rule Violations (`test-business-rules.csv`):**

```csv
waybill_id,waybill_date,delivery_date,project_id,supplier_id,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status
WB-ERR-001,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,0.3,מ"ק,150.75,45.23,ILS,Delivered
WB-ERR-002,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,60.0,מ"ק,150.75,9045.00,ILS,Delivered
WB-ERR-003,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,2000.00,ILS,Delivered
WB-ERR-004,2024-01-15,2024-01-10,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,1582.87,ILS,Delivered
```

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-business-rules.csv" \
  http://localhost:5000/api/waybills/import
```

**Expected Errors:**
- Row 1: Quantity below minimum (0.3 < 0.5)
- Row 2: Quantity above maximum (60 > 50)
- Row 3: total_amount mismatch (2000 ≠ 10.5 × 150.75)
- Row 4: delivery_date < waybill_date

**✅ PASS Criteria:**
- All business rule violations detected
- Appropriate error messages
- Rows not imported

---

### Test 4.4: Duplicate Detection

**Create CSV with Duplicate waybill_id (`test-duplicate.csv`):**

```csv
waybill_id,waybill_date,delivery_date,project_id,supplier_id,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status
WB-DUP-001,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,1582.87,ILS,Delivered
WB-DUP-001,2024-01-16,2024-01-21,PRJ001,SUP001,B30,בטון ב-30,11.0,מ"ק,150.75,1658.25,ILS,Delivered
```

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-duplicate.csv" \
  http://localhost:5000/api/waybills/import
```

**Expected Behavior:**
- First row: Created
- Second row: Updated (upsert logic) OR error if duplicate detection is strict

**Verify:**
```sql
SELECT * FROM Waybills WHERE Id = 'WB-DUP-001';
-- Should see only one record with latest data
```

**✅ PASS Criteria:**
- Upsert logic works OR duplicate detected
- No duplicate records in database

---

### Test 4.5: Hebrew Text Support

**Create CSV with Hebrew Text (`test-hebrew.csv`):**

```csv
waybill_id,waybill_date,delivery_date,project_id,supplier_id,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status,delivery_address
WB-HEB-001,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,1582.87,ILS,Delivered,רחוב הרצל 1 תל אביב
```

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-hebrew.csv" \
  http://localhost:5000/api/waybills/import
```

**Verify in Database:**
```sql
SELECT ProductName, DeliveryAddress FROM Waybills WHERE Id = 'WB-HEB-001';
-- Should see Hebrew text correctly stored
```

**Verify via API:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/WB-HEB-001"
```

**Expected:** Hebrew text in response should be correct

**✅ PASS Criteria:**
- Hebrew text stored correctly in database
- Hebrew text retrieved correctly via API
- No character corruption

---

### Test 4.6: Large File Import

**Create CSV with 100+ rows (if possible)**

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-large.csv" \
  http://localhost:5000/api/waybills/import
```

**Expected:**
- All rows processed
- Performance acceptable (< 30 seconds for 100 rows)
- Errors reported for invalid rows

**✅ PASS Criteria:**
- All valid rows imported
- Performance acceptable
- Error handling works

---

## Section 5: API Endpoint Testing

### Test 5.1: GET /api/waybills - Basic List

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills"
```

**Expected Response:**
```json
{
  "items": [...],
  "totalCount": 10,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

**✅ PASS Criteria:**
- Returns paginated list
- TotalCount, PageNumber, PageSize in response

---

### Test 5.2: GET /api/waybills - Date Filtering

**Test Waybill Date Range:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?dateFrom=2024-01-01&dateTo=2024-01-31"
```

**Test Delivery Date Range:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?deliveryDateFrom=2024-01-01&deliveryDateTo=2024-01-31"
```

**Expected:** Only waybills in date range returned

**✅ PASS Criteria:**
- Date filtering works correctly
- Only waybills in range returned

---

### Test 5.3: GET /api/waybills - Status Filtering

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?status=Delivered"
```

**Expected:** Only waybills with status "Delivered" returned

**✅ PASS Criteria:**
- Status filtering works
- Only matching waybills returned

---

### Test 5.4: GET /api/waybills - Project Filtering

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?projectId=PRJ001"
```

**Expected:** Only waybills for project PRJ001 returned

**✅ PASS Criteria:**
- Project filtering works
- Only matching waybills returned

---

### Test 5.5: GET /api/waybills - Supplier Filtering

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?supplierId=SUP001"
```

**Expected:** Only waybills for supplier SUP001 returned

**✅ PASS Criteria:**
- Supplier filtering works
- Only matching waybills returned

---

### Test 5.6: GET /api/waybills - Product Code Filtering

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?productCode=B30"
```

**Expected:** Only waybills with product code B30 returned

**✅ PASS Criteria:**
- Product code filtering works
- Only matching waybills returned

---

### Test 5.7: GET /api/waybills - Text Search (Hebrew)

**Test:**
```bash
# Search for Hebrew project name
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?searchText=פרויקט"
```

**Expected:** Waybills with matching Hebrew text returned

**✅ PASS Criteria:**
- Hebrew text search works
- Case-insensitive
- Unicode-aware

---

### Test 5.8: GET /api/waybills/{id}

**Test Existing Waybill:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/WB-2024-001"
```

**Expected:** Returns full waybill details

**Test Non-Existent Waybill:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/WB-NOT-EXISTS"
```

**Expected:** 404 Not Found

**✅ PASS Criteria:**
- Existing waybill returned
- Non-existent returns 404

---

### Test 5.9: GET /api/waybills/summary

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/summary"
```

**Expected Response:**
```json
{
  "totalQuantityByStatus": {...},
  "totalAmountByStatus": {...},
  "monthlyBreakdown": [...],
  "topSuppliers": [...],
  "projectTotals": [...],
  "disputedCount": 0,
  "cancelledCount": 0,
  "disputedPercentage": 0,
  "cancelledPercentage": 0
}
```

**✅ PASS Criteria:**
- All summary fields present
- Calculations correct

---

### Test 5.10: GET /api/waybills/summary - Date Range

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/summary?dateFrom=2024-01-01&dateTo=2024-01-31"
```

**Expected:** Summary only includes waybills in date range

**✅ PASS Criteria:**
- Date range filtering works
- Summary calculations correct

---

### Test 5.11: PATCH /api/waybills/{id}/status

**Test Valid Transitions:**

1. **PENDING → DELIVERED:**
```bash
curl -X PATCH \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Delivered",
    "notes": "Successfully delivered"
  }' \
  "http://localhost:5000/api/waybills/WB-2024-001/status"
```

**Expected:** 200 OK

2. **DELIVERED → DISPUTED:**
```bash
curl -X PATCH \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Disputed",
    "notes": "Quantity discrepancy"
  }' \
  "http://localhost:5000/api/waybills/WB-2024-001/status"
```

**Expected:** 200 OK

**Test Invalid Transitions:**

3. **CANCELLED → DELIVERED:**
```bash
# First, cancel a waybill
curl -X PATCH \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d '{"status": "Cancelled"}' \
  "http://localhost:5000/api/waybills/WB-2024-002/status"

# Then try to change it
curl -X PATCH \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d '{"status": "Delivered"}' \
  "http://localhost:5000/api/waybills/WB-2024-002/status"
```

**Expected:** 400 Bad Request with error message

**✅ PASS Criteria:**
- Valid transitions succeed
- Invalid transitions rejected with clear error

---

### Test 5.12: GET /api/projects/{id}/waybills

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/projects/PRJ001/waybills"
```

**Expected:** Returns all waybills for project PRJ001 (tenant-scoped)

**✅ PASS Criteria:**
- Returns waybills for project
- Tenant-scoped

---

### Test 5.13: GET /api/suppliers/{id}/summary

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/suppliers/SUP001/summary"
```

**Expected Response:**
```json
{
  "supplierId": "SUP001",
  "supplierName": "Supplier Name",
  "totalDeliveries": 10,
  "totalQuantity": 100.5,
  "totalAmount": 15000.75,
  "averageQuantityPerDelivery": 10.05,
  "statusBreakdown": {
    "Pending": 2,
    "Delivered": 7,
    "Cancelled": 0,
    "Disputed": 1
  }
}
```

**✅ PASS Criteria:**
- Returns supplier statistics
- Tenant-scoped

---

## Section 6: Concurrency Testing

### Test 6.1: Single-User Execution

**Test Steps:**

1. **Open two terminal windows**

2. **Terminal 1 - Start report generation:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/reports/generate-monthly-report"
```

3. **Terminal 2 - Immediately try again:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/reports/generate-monthly-report"
```

**Expected:**
- Terminal 1: 200 OK (after 3 seconds)
- Terminal 2: 409 Conflict immediately

**Response from Terminal 2:**
```json
{
  "error": "Report generation is already in progress",
  "message": "Another monthly report generation is currently in progress..."
}
```

4. **Wait for Terminal 1 to complete, then try again:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/reports/generate-monthly-report"
```

**Expected:** 200 OK

**✅ PASS Criteria:**
- First request succeeds
- Second request returns 409 Conflict
- After completion, new request succeeds

---

### Test 6.2: Optimistic Locking

**Test Steps:**

1. **Get waybill and save Version:**
```bash
RESPONSE=$(curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/WB-2024-001")

# Extract version (you'll need to parse JSON)
VERSION="..."  # Extract from response
```

2. **Update with correct Version:**
```bash
curl -X PUT \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d "{
    \"version\": \"$VERSION\",
    \"waybillDate\": \"2024-01-15\",
    \"deliveryDate\": \"2024-01-20\",
    ...
  }" \
  "http://localhost:5000/api/waybills/WB-2024-001"
```

**Expected:** 200 OK

3. **Get waybill again (new Version):**
```bash
RESPONSE=$(curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/WB-2024-001")

NEW_VERSION="..."  # Extract new version
```

4. **Try to update with old Version:**
```bash
curl -X PUT \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d "{
    \"version\": \"$VERSION\",
    ...
  }" \
  "http://localhost:5000/api/waybills/WB-2024-001"
```

**Expected Response:**
- **Status Code:** 409 Conflict
- **Response Body:**
```json
{
  "error": "Concurrent update detected",
  "message": "Waybill with ID 'WB-2024-001' was modified by another user. Please refresh and try again.",
  "suggestion": "The resource was modified by another user. Please refresh the data, review the changes, and try updating again with the new version."
}
```

**✅ PASS Criteria:**
- Update with correct version succeeds
- Update with old version returns 409 Conflict
- Error message is clear

---

## Section 7: Message Broker Testing

### Test 7.1: Event Publishing

**Test Steps:**

1. **Import CSV file:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-waybills.csv" \
  http://localhost:5000/api/waybills/import
```

2. **Check RabbitMQ Management UI:**
- Open http://localhost:15672
- Login: admin/admin
- Go to "Queues" tab
- Look for "waybill-imported" queue
- Check message count

**Expected:** Event published to queue

**✅ PASS Criteria:**
- Event appears in queue
- Message count increases

---

### Test 7.2: Event Consumption

**Test Steps:**

1. **Import CSV file:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -F "file=@test-waybills.csv" \
  http://localhost:5000/api/waybills/import
```

2. **Check application logs:**
```bash
# Check console output or log files
# Should see log entry about event consumption
```

**Expected:** Log entry showing event was consumed

**✅ PASS Criteria:**
- Event consumed
- Log entry present

---

## Section 8: Error Handling Testing

### Test 8.1: Invalid Data

**Test:**
```bash
curl -X POST \
  -H "X-Tenant-ID: TENANT001" \
  -H "Content-Type: application/json" \
  -d '{"invalid": "json"}' \
  "http://localhost:5000/api/waybills/WB-2024-001/status"
```

**Expected:** 400 Bad Request with validation errors

**✅ PASS Criteria:**
- Returns 400 Bad Request
- Validation errors in response

---

### Test 8.2: Missing Resources

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills/NON-EXISTENT"
```

**Expected:** 404 Not Found

**✅ PASS Criteria:**
- Returns 404 Not Found
- Clear error message

---

### Test 8.3: Database Errors

**Test:**
1. Stop SQL Server: `docker stop gekko-sqlserver`
2. Make request:
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills"
```

**Expected:** 500 Internal Server Error

3. Start SQL Server: `docker start gekko-sqlserver`

**✅ PASS Criteria:**
- Appropriate error handling
- Error message doesn't leak sensitive info

---

## Section 9: Performance Testing

### Test 9.1: Large Dataset

**Test:**
1. Import 100+ waybills
2. Query with filters:
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?status=Delivered"
```

**Expected:** Response time < 2 seconds

**✅ PASS Criteria:**
- Performance acceptable
- Response time reasonable

---

### Test 9.2: Pagination

**Test:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills?pageSize=10&pageNumber=1"
```

**Expected:**
- Only 10 results returned
- TotalPages calculated correctly

**✅ PASS Criteria:**
- Pagination works
- TotalPages correct

---

## Testing Complete

After completing all tests, review the TESTING_CHECKLIST.md to ensure all items are checked.

**Next Steps:**
- Review any failures
- Document issues in VERIFICATION_REPORT.md
- Fix any critical issues
- Re-test after fixes

---

**Last Updated:** 2024-02-01
