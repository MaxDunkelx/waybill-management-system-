# Waybill Management System - Testing Checklist

Use this checklist to track your testing progress. Check off each item as you complete it.

---

## Prerequisites & Setup

- [ ] Docker Desktop installed and running
- [ ] .NET 10.0 SDK installed
- [ ] Docker services started (SQL Server, RabbitMQ, Redis)
- [ ] Database migrations applied
- [ ] API started and accessible
- [ ] Swagger UI accessible at http://localhost:5000
- [ ] Test tenants created (TENANT001, TENANT002, TENANT003)

---

## Multi-Tenant Isolation Testing (CRITICAL)

### Test 3.1: Tenant Header Required
- [ ] Request without X-Tenant-ID header returns 400 Bad Request
- [ ] Error message is clear and helpful

### Test 3.2: Tenant Isolation - Waybills
- [ ] Import CSV with TENANT001
- [ ] Query waybills with TENANT001 → sees data
- [ ] Query waybills with TENANT002 → sees NO data
- [ ] Get waybill by ID with wrong tenant → returns 404

### Test 3.3: Tenant Isolation - Projects
- [ ] Query projects with TENANT001 → sees data
- [ ] Query same project ID with TENANT002 → sees empty/404

### Test 3.4: Tenant Isolation - Suppliers
- [ ] Query supplier summary with TENANT001 → sees data
- [ ] Query same supplier ID with TENANT002 → returns 404

### Test 3.5: Tenant Isolation - Summary
- [ ] Get summary with TENANT001 → sees TENANT001 data only
- [ ] Get summary with TENANT002 → sees TENANT002 data only

### Test 3.6: Cross-Tenant Data Access Prevention
- [ ] Try to update waybill from TENANT001 using TENANT002 header → returns 404
- [ ] No data leakage between tenants

**Overall Multi-Tenant Isolation:** ✅ / ❌

---

## CSV Import Testing

### Test 4.1: Successful Import
- [ ] Import valid CSV file
- [ ] Success count matches valid rows
- [ ] No errors reported
- [ ] Data appears in database
- [ ] Hebrew text stored correctly

### Test 4.2: Validation - Required Fields
- [ ] Missing waybill_id → error reported
- [ ] Missing other required fields → errors reported
- [ ] Clear error messages

### Test 4.3: Validation - Business Rules
- [ ] Quantity < 0.5 → error
- [ ] Quantity > 50 → error
- [ ] total_amount ≠ quantity × unit_price → error
- [ ] delivery_date < waybill_date → error
- [ ] All errors clearly reported

### Test 4.4: Duplicate Detection
- [ ] Duplicate waybill_id → upsert works OR error reported
- [ ] No duplicate records in database

### Test 4.5: Hebrew Text Support
- [ ] Hebrew text in CSV imported correctly
- [ ] Hebrew text stored correctly in database
- [ ] Hebrew text retrieved correctly via API
- [ ] No character corruption

### Test 4.6: Large File Import
- [ ] 100+ rows imported successfully
- [ ] Performance acceptable (< 30 seconds)
- [ ] Error handling works for invalid rows

**Overall CSV Import:** ✅ / ❌

---

## API Endpoint Testing

### Test 5.1: GET /api/waybills - Basic List
- [ ] Returns paginated list
- [ ] TotalCount, PageNumber, PageSize in response
- [ ] TotalPages calculated correctly

### Test 5.2: GET /api/waybills - Date Filtering
- [ ] waybill_date range filtering works
- [ ] delivery_date range filtering works
- [ ] Only waybills in range returned

### Test 5.3: GET /api/waybills - Status Filtering
- [ ] Status filter works (Pending, Delivered, Cancelled, Disputed)
- [ ] Only matching waybills returned

### Test 5.4: GET /api/waybills - Project Filtering
- [ ] Project filter works
- [ ] Only waybills for project returned

### Test 5.5: GET /api/waybills - Supplier Filtering
- [ ] Supplier filter works
- [ ] Only waybills for supplier returned

### Test 5.6: GET /api/waybills - Product Code Filtering
- [ ] Product code filter works
- [ ] Only matching waybills returned

### Test 5.7: GET /api/waybills - Text Search (Hebrew)
- [ ] Hebrew text search works
- [ ] Case-insensitive
- [ ] Unicode-aware

### Test 5.8: GET /api/waybills/{id}
- [ ] Existing waybill returned
- [ ] Non-existent waybill returns 404
- [ ] Full waybill details in response

### Test 5.9: GET /api/waybills/summary
- [ ] All summary fields present
- [ ] Calculations correct
- [ ] Tenant-scoped

### Test 5.10: GET /api/waybills/summary - Date Range
- [ ] Date range filtering works
- [ ] Summary calculations correct

### Test 5.11: PATCH /api/waybills/{id}/status
- [ ] PENDING → DELIVERED works
- [ ] PENDING → CANCELLED works
- [ ] DELIVERED → DISPUTED works
- [ ] CANCELLED → anything returns 400
- [ ] Invalid transitions rejected with clear error

### Test 5.12: GET /api/projects/{id}/waybills
- [ ] Returns waybills for project
- [ ] Tenant-scoped

### Test 5.13: GET /api/suppliers/{id}/summary
- [ ] Returns supplier statistics
- [ ] Tenant-scoped
- [ ] All statistics calculated correctly

**Overall API Endpoints:** ✅ / ❌

---

## Concurrency Testing

### Test 6.1: Single-User Execution
- [ ] First report generation request succeeds
- [ ] Second concurrent request returns 409 Conflict
- [ ] After first completes, new request succeeds
- [ ] Error message is clear

### Test 6.2: Optimistic Locking
- [ ] Update with correct version succeeds
- [ ] Update with old version returns 409 Conflict
- [ ] Error message explains concurrent update
- [ ] Suggestion to refresh data provided

**Overall Concurrency:** ✅ / ❌

---

## Message Broker Testing

### Test 7.1: Event Publishing
- [ ] Event published after CSV import
- [ ] Event appears in RabbitMQ queue
- [ ] Message count increases

### Test 7.2: Event Consumption
- [ ] Event consumed by background service
- [ ] Log entry shows event processing
- [ ] No errors in consumption

**Overall Message Broker:** ✅ / ❌

---

## Error Handling Testing

### Test 8.1: Invalid Data
- [ ] Invalid JSON returns 400 Bad Request
- [ ] Validation errors in response
- [ ] Error messages are helpful

### Test 8.2: Missing Resources
- [ ] Non-existent waybill returns 404
- [ ] Error message is clear

### Test 8.3: Database Errors
- [ ] Database unavailable → appropriate error handling
- [ ] Error message doesn't leak sensitive info
- [ ] System recovers when database available

**Overall Error Handling:** ✅ / ❌

---

## Performance Testing

### Test 9.1: Large Dataset
- [ ] 100+ waybills imported successfully
- [ ] Query performance acceptable (< 2 seconds)
- [ ] Filtering performance acceptable

### Test 9.2: Pagination
- [ ] Pagination works correctly
- [ ] Only requested page size returned
- [ ] TotalPages calculated correctly

**Overall Performance:** ✅ / ❌

---

## Documentation & Code Quality

- [ ] XML documentation comments on all public methods
- [ ] README.md is comprehensive
- [ ] Swagger UI displays correctly
- [ ] Code comments explain complex logic
- [ ] Error messages are clear and helpful

**Overall Documentation:** ✅ / ❌

---

## Hebrew Text Support

- [ ] UTF-8 encoding configured
- [ ] All text columns use nvarchar in database
- [ ] CSV import handles Hebrew correctly
- [ ] API responses include Hebrew text correctly
- [ ] Swagger displays Hebrew correctly

**Overall Hebrew Support:** ✅ / ❌

---

## Security Review

- [ ] Multi-tenant isolation enforced
- [ ] Tenant ID never from user input
- [ ] SQL injection prevented (EF Core)
- [ ] Input validation on all endpoints
- [ ] Error messages don't leak info
- [ ] Optimistic locking prevents conflicts
- [ ] Distributed locking prevents exhaustion

**Overall Security:** ✅ / ❌

---

## Final Assessment

### Critical Requirements
- [ ] Multi-tenant isolation works correctly
- [ ] CSV import works with validation
- [ ] All API endpoints work
- [ ] Business rules enforced
- [ ] Concurrency handling works

### Overall System Status
- [ ] **PASS** - All critical requirements met
- [ ] **PASS WITH ISSUES** - Minor issues found
- [ ] **FAIL** - Critical issues found

---

## Issues Found

Document any issues found during testing:

1. **Issue:** [Description]
   - **Severity:** Critical / High / Medium / Low
   - **Status:** Open / Fixed / Deferred

2. **Issue:** [Description]
   - **Severity:** Critical / High / Medium / Low
   - **Status:** Open / Fixed / Deferred

---

## Sign-Off

**Tester Name:** _________________  
**Date:** _________________  
**Status:** ✅ PASS / ❌ FAIL  
**Notes:** _________________

---

**Last Updated:** 2024-02-01
