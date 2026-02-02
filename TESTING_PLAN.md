# Comprehensive Testing Plan - Automatic Migrations & System Verification

**Date**: Testing Plan Created  
**Purpose**: Verify automatic migrations work correctly and system is fully functional

---

## 🎯 Testing Objectives

1. ✅ Verify automatic migrations run on startup
2. ✅ Verify migrations work with fresh database (no data)
3. ✅ Verify migrations work with existing database (migrations already applied)
4. ✅ Verify database schema is created correctly
5. ✅ Verify application starts successfully after migrations
6. ✅ Verify API endpoints work correctly
7. ✅ Verify RabbitMQ connectivity and message flow
8. ✅ Verify all services start in correct order
9. ✅ Verify error handling (migrations fail scenario)

---

## 📋 Pre-Testing Checklist

Before starting tests, ensure:
- [ ] Docker Desktop is running
- [ ] No existing containers are running (clean state)
- [ ] Code changes are saved
- [ ] Automatic migrations code is added to Program.cs

---

## 🧪 Test 1: Fresh Database (No Existing Data)

### Objective
Verify automatic migrations create database and schema from scratch.

### Steps

1. **Clean up everything (including volumes)**:
   ```bash
   docker-compose down -v
   ```
   This removes all containers AND volumes (fresh start).

2. **Start all services**:
   ```bash
   docker-compose up --build
   ```

3. **Monitor logs for migration messages**:
   Look for these log messages in backend container:
   ```
   ==========================================
   Applying database migrations...
   ==========================================
   ==========================================
   Database migrations applied successfully
   ==========================================
   ```

4. **Verify database exists**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -Q "SELECT name FROM sys.databases WHERE name = 'WaybillManagementDB'"
   ```
   **Expected**: Should return `WaybillManagementDB`

5. **Verify tables are created**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT name FROM sys.tables ORDER BY name"
   ```
   **Expected**: Should return:
   - `Tenants`
   - `Projects`
   - `Suppliers`
   - `Waybills`
   - `Jobs`
   - `__EFMigrationsHistory`

6. **Verify migrations history**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId"
   ```
   **Expected**: Should show all 4 migrations:
   - `20260201084155_InitialCreate`
   - `20260201131133_ChangeSupplierToCompositeKey`
   - `20260201172527_AddErpSyncStatus`
   - `20260201173806_AddJobEntity`

### Success Criteria
- ✅ Logs show "Database migrations applied successfully"
- ✅ Database `WaybillManagementDB` exists
- ✅ All 6 tables are created
- ✅ All 4 migrations are recorded in `__EFMigrationsHistory`
- ✅ Application starts and is accessible

---

## 🧪 Test 2: Existing Database (Migrations Already Applied)

### Objective
Verify automatic migrations are idempotent (safe to run multiple times).

### Steps

1. **Stop containers (keep volumes)**:
   ```bash
   docker-compose down
   ```
   This stops containers but keeps volumes (data persists).

2. **Start services again**:
   ```bash
   docker-compose up
   ```

3. **Monitor logs**:
   Look for migration messages:
   ```
   ==========================================
   Applying database migrations...
   ==========================================
   ==========================================
   Database migrations applied successfully
   ==========================================
   ```
   **Expected**: Should see success message (no errors)

4. **Verify migrations history unchanged**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT COUNT(*) as MigrationCount FROM __EFMigrationsHistory"
   ```
   **Expected**: Should still show 4 migrations (no new migrations added)

5. **Verify data persists**:
   If you had data before, check it's still there:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT COUNT(*) as WaybillCount FROM Waybills"
   ```
   **Expected**: Data should still be there (migrations don't delete data)

### Success Criteria
- ✅ Logs show "Database migrations applied successfully"
- ✅ No errors in logs
- ✅ Migration count unchanged (still 4)
- ✅ Existing data is preserved
- ✅ Application starts successfully

---

## 🧪 Test 3: Database Schema Verification

### Objective
Verify all tables have correct structure (columns, indexes, foreign keys).

### Steps

1. **Verify Tenants table**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' ORDER BY ORDINAL_POSITION"
   ```
   **Expected**: Should show: `Id`, `Name`, `CreatedAt`

2. **Verify Waybills table**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Waybills' ORDER BY ORDINAL_POSITION"
   ```
   **Expected**: Should show all waybill columns including `ErpSyncStatus`, `Version`

3. **Verify indexes exist**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('Waybills') AND name IS NOT NULL"
   ```
   **Expected**: Should show indexes for `TenantId`, `ProjectId`, `Status`, etc.

4. **Verify foreign keys exist**:
   ```bash
   docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P YourStrong@Passw0rd -C \
     -d WaybillManagementDB \
     -Q "SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('Waybills')"
   ```
   **Expected**: Should show foreign keys to `Tenants`, `Projects`, `Suppliers`

### Success Criteria
- ✅ All tables have correct columns
- ✅ All indexes are created
- ✅ All foreign keys are created
- ✅ Schema matches migration files

---

## 🧪 Test 4: Application Startup & API Endpoints

### Objective
Verify application starts correctly and API endpoints work.

### Steps

1. **Check application logs**:
   ```bash
   docker-compose logs backend | tail -50
   ```
   **Expected**: Should see:
   - Migration success message
   - "Now listening on: http://[::]:80"
   - No errors

2. **Test health endpoint** (if exists):
   ```bash
   curl http://localhost:5001/api/TenantTest/test -H "X-Tenant-ID: TENANT001"
   ```
   **Expected**: Should return JSON response

3. **Test Swagger UI**:
   - Open browser: `http://localhost:5001/swagger`
   - **Expected**: Swagger UI should load

4. **Test tenant endpoint**:
   ```bash
   curl http://localhost:5001/api/Waybills -H "X-Tenant-ID: TENANT001"
   ```
   **Expected**: Should return empty array `[]` (no waybills yet)

5. **Test without tenant header**:
   ```bash
   curl http://localhost:5001/api/Waybills
   ```
   **Expected**: Should return `400 Bad Request` (tenant required)

### Success Criteria
- ✅ Application starts without errors
- ✅ Swagger UI is accessible
- ✅ API endpoints respond correctly
- ✅ Tenant validation works (400 without header)

---

## 🧪 Test 5: RabbitMQ Connectivity & Message Flow

### Objective
Verify RabbitMQ is connected and messages flow correctly.

### Steps

1. **Access RabbitMQ Management UI**:
   - Open browser: `http://localhost:15672`
   - Login: `admin` / `admin`
   - **Expected**: Should see RabbitMQ dashboard

2. **Check connections**:
   - Go to "Connections" tab
   - **Expected**: Should see connections from backend (MessagePublisher, MessageConsumer)

3. **Check queues**:
   - Go to "Queues" tab
   - **Expected**: Should see queue(s) created by the application

4. **Test message flow** (if CSV import endpoint exists):
   - Import a CSV file via API
   - Watch RabbitMQ UI
   - **Expected**: Message should appear in queue and be consumed

5. **Check logs for RabbitMQ**:
   ```bash
   docker-compose logs backend | grep -i rabbitmq
   ```
   **Expected**: Should see connection messages (no errors)

### Success Criteria
- ✅ RabbitMQ UI is accessible
- ✅ Connections are established
- ✅ Queues are created
- ✅ Messages flow correctly (if tested)

---

## 🧪 Test 6: Service Startup Order

### Objective
Verify services start in correct order (dependencies respected).

### Steps

1. **Check service startup order in logs**:
   ```bash
   docker-compose logs | grep -E "started|ready|healthy"
   ```
   **Expected**: Should see:
   - SQL Server becomes healthy first
   - RabbitMQ becomes healthy
   - Redis becomes healthy
   - Backend starts last (after dependencies)

2. **Check depends_on configuration**:
   Review `docker-compose.yml`:
   ```yaml
   depends_on:
     sqlserver:
       condition: service_healthy
     rabbitmq:
       condition: service_healthy
     redis:
       condition: service_healthy
   ```
   **Expected**: Backend should wait for all dependencies

3. **Test startup time**:
   ```bash
   time docker-compose up -d
   ```
   **Expected**: Backend should start after dependencies are healthy

### Success Criteria
- ✅ SQL Server starts first
- ✅ RabbitMQ and Redis start
- ✅ Backend starts last (after all dependencies are healthy)
- ✅ No connection errors in logs

---

## 🧪 Test 7: Error Handling (Migrations Fail)

### Objective
Verify application fails gracefully if migrations fail.

### Steps

1. **Simulate migration failure** (optional - advanced test):
   - Temporarily break a migration file
   - Try to start application
   - **Expected**: Application should NOT start, error should be logged

2. **Check error logging**:
   If migration fails, logs should show:
   ```
   ==========================================
   ERROR: Failed to apply database migrations
   Application will not start until migrations succeed
   ==========================================
   ```

### Success Criteria
- ✅ Application doesn't start if migrations fail
- ✅ Clear error message is logged
- ✅ Fail-fast approach works correctly

---

## 📊 Test Results Summary

After completing all tests, fill in this summary:

| Test | Status | Notes |
|------|--------|-------|
| Test 1: Fresh Database | ⬜ | |
| Test 2: Existing Database | ⬜ | |
| Test 3: Schema Verification | ⬜ | |
| Test 4: API Endpoints | ⬜ | |
| Test 5: RabbitMQ | ⬜ | |
| Test 6: Service Order | ⬜ | |
| Test 7: Error Handling | ⬜ | |

---

## 🚀 Quick Verification Commands

### Check all services are running:
```bash
docker-compose ps
```

### Check backend logs:
```bash
docker-compose logs backend | tail -100
```

### Check database:
```bash
docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P YourStrong@Passw0rd -C \
  -d WaybillManagementDB \
  -Q "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
```

### Check migrations:
```bash
docker exec gekko-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P YourStrong@Passw0rd -C \
  -d WaybillManagementDB \
  -Q "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
```

---

## ✅ Final Checklist

Before considering testing complete:
- [ ] All 7 tests passed
- [ ] No errors in logs
- [ ] Database schema is correct
- [ ] API endpoints work
- [ ] RabbitMQ is connected
- [ ] Services start in correct order
- [ ] Application is ready for interviewers

---

**Next Steps**: Run tests systematically and report results.
