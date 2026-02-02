# Waybill Management System - Quick Start Guide

Get up and running with the Waybill Management System in 5 minutes.

---

## Step 1: Start Services (1 minute)

```bash
# Navigate to project root
cd /Users/maxdunkel/gekko

# Start all services (SQL Server, RabbitMQ, Redis)
docker-compose up -d

# Wait for services to be ready (about 30 seconds)
docker-compose ps
```

**Verify Services:**
- SQL Server: Check logs with `docker logs gekko-sqlserver`
- RabbitMQ: Open http://localhost:15672 (admin/admin)
- Redis: `docker exec -it gekko-redis redis-cli ping` (should return "PONG")

---

## Step 2: Setup Database (1 minute)

```bash
# Navigate to backend directory
cd backend

# Apply database migrations
dotnet ef database update --project .

# Create test tenant (using SQL)
# Connect to: localhost,1433
# User: sa, Password: YourStrong@Passw0rd
# Database: WaybillManagementDB

# Run this SQL:
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd -d WaybillManagementDB -Q "INSERT INTO Tenants (Id, Name, CreatedAt) VALUES ('TENANT001', 'Test Tenant', GETUTCDATE())"
```

**Or use SQL Server Management Studio:**
```sql
USE WaybillManagementDB;
INSERT INTO Tenants (Id, Name, CreatedAt) VALUES ('TENANT001', 'Test Tenant', GETUTCDATE());
```

---

## Step 3: Start API (30 seconds)

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

---

## Step 4: First Test - Import CSV (1 minute)

**Create test CSV file (`test-waybills.csv`):**

```csv
waybill_id,waybill_date,delivery_date,project_id,supplier_id,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status,vehicle_number,driver_name,delivery_address,notes
WB-2024-001,2024-01-15,2024-01-20,PRJ001,SUP001,B30,בטון ב-30,10.5,מ"ק,150.75,1582.87,ILS,Delivered,123-45-678,יוסי כהן,רחוב הרצל 1 תל אביב,Test note
WB-2024-002,2024-01-16,2024-01-21,PRJ001,SUP002,B25,בטון ב-25,5.0,מ"ק,140.50,702.50,ILS,Pending,456-78-901,דני לוי,רחוב דיזנגוף 10 תל אביב,
```

**Import CSV:**
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
  "warnings": []
}
```

**✅ Success!** Your first waybills have been imported.

---

## Step 5: Second Test - Query Waybills (30 seconds)

**Get all waybills:**
```bash
curl -H "X-Tenant-ID: TENANT001" \
  "http://localhost:5000/api/waybills"
```

**Expected Response:**
```json
{
  "items": [
    {
      "id": "WB-2024-001",
      "waybillDate": "2024-01-15",
      "deliveryDate": "2024-01-20",
      "projectId": "PRJ001",
      "projectName": "Project 1",
      "supplierId": "SUP001",
      "supplierName": "Supplier 1",
      "productCode": "B30",
      "productName": "בטון ב-30",
      "quantity": 10.5,
      "unit": "מ\"ק",
      "unitPrice": 150.75,
      "totalAmount": 1582.87,
      "currency": "ILS",
      "status": "Delivered",
      "vehicleNumber": "123-45-678",
      "driverName": "יוסי כהן",
      "deliveryAddress": "רחוב הרצל 1 תל אביב",
      "notes": "Test note"
    },
    ...
  ],
  "totalCount": 2,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

**✅ Success!** You can query waybills.

---

## Step 6: Third Test - Verify Tenant Isolation (30 seconds)

**Test with different tenant:**
```bash
# Query with TENANT002 (should see NO data)
curl -H "X-Tenant-ID: TENANT002" \
  "http://localhost:5000/api/waybills"
```

**Expected Response:**
```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 0
}
```

**✅ Success!** Tenant isolation is working - TENANT002 sees no data from TENANT001.

---

## Step 7: Explore Swagger UI (1 minute)

1. **Open Swagger UI:**
   - Navigate to http://localhost:5000
   - You should see all API endpoints

2. **Authorize:**
   - Click "Authorize" button
   - Enter `TENANT001` in the "TenantIdHeader" field
   - Click "Authorize"
   - Click "Close"

3. **Test an endpoint:**
   - Expand `GET /api/waybills`
   - Click "Try it out"
   - Click "Execute"
   - See the response

**✅ Success!** You can use Swagger UI to test the API.

---

## Next Steps

Now that you have the system running, you can:

1. **Read the full testing guide:**
   - See `TESTING_GUIDE.md` for comprehensive test cases

2. **Review the verification report:**
   - See `VERIFICATION_REPORT.md` for system review

3. **Use the testing checklist:**
   - See `TESTING_CHECKLIST.md` to track your testing progress

4. **Import more data:**
   - Create larger CSV files
   - Test different scenarios

5. **Test all endpoints:**
   - Use Swagger UI or curl commands
   - Test filtering, pagination, status updates

---

## Common Issues

### Issue: Docker services won't start
**Solution:**
```bash
# Check if ports are already in use
docker-compose down
docker-compose up -d
```

### Issue: Database migration fails
**Solution:**
```bash
# Ensure SQL Server is running
docker ps | grep sqlserver

# Check connection string in appsettings.json
# Verify password matches docker-compose.yml
```

### Issue: API won't start
**Solution:**
```bash
# Check .NET SDK version
dotnet --version  # Should be 10.0.x

# Restore packages
dotnet restore

# Build project
dotnet build
```

### Issue: CSV import fails
**Solution:**
- Ensure CSV file is UTF-8 encoded
- Check CSV has header row
- Verify all required fields are present
- Check tenant ID is correct

---

## Quick Commands Reference

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f

# Apply migrations
cd backend && dotnet ef database update --project .

# Start API
cd backend && dotnet run

# Test tenant endpoint
curl -H "X-Tenant-ID: TENANT001" http://localhost:5000/api/tenant/test

# Import CSV
curl -X POST -H "X-Tenant-ID: TENANT001" -F "file=@test.csv" http://localhost:5000/api/waybills/import

# Get waybills
curl -H "X-Tenant-ID: TENANT001" http://localhost:5000/api/waybills
```

---

## Support

If you encounter issues:

1. Check the logs:
   - API logs in console
   - Docker logs: `docker-compose logs`

2. Review documentation:
   - `README.md` - Setup instructions
   - `TESTING_GUIDE.md` - Comprehensive testing
   - `VERIFICATION_REPORT.md` - System review

3. Verify prerequisites:
   - Docker Desktop running
   - .NET 10.0 SDK installed
   - Services started and healthy

---

**Last Updated:** 2024-02-01  
**System Version:** 1.0
