using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Models;
using WaybillManagementSystem.Models.Enums;
using WaybillManagementSystem.Models.Events;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for importing waybill data from CSV files.
/// 
/// IMPLEMENTATION DETAILS:
/// This service uses CsvHelper library to parse CSV files with proper support
/// for Hebrew text and UTF-8 encoding. It handles date parsing, error collection,
/// and provides comprehensive feedback about the import process.
/// 
/// CSV HELPER CONFIGURATION:
/// The service configures CsvHelper with:
/// - UTF-8 encoding for Hebrew text support
/// - Header row detection and mapping
/// - Flexible date parsing (multiple formats)
/// - Error handling and bad data detection
/// - Culture-invariant number parsing
/// 
/// ENCODING CONSIDERATIONS:
/// Hebrew text requires UTF-8 encoding to properly represent Unicode characters
/// (Hebrew Unicode range: U+0590 to U+05FF). The service explicitly sets:
/// - StreamReader encoding to UTF-8
/// - CsvHelper to use UTF-8
/// - Proper handling of BOM (Byte Order Mark) if present
/// 
/// This ensures that Hebrew characters in product names, addresses, driver names,
/// units, and notes are correctly parsed from the CSV file.
/// 
/// DATE PARSING STRATEGY:
/// The service supports multiple date formats commonly used in CSV files:
/// - YYYY-MM-DD (ISO 8601 format, recommended)
/// - DD/MM/YYYY (common in some regions)
/// - MM/DD/YYYY (US format)
/// 
/// CsvHelper will attempt to parse dates using these formats automatically.
/// If a date cannot be parsed, it will be captured as a string and validation
/// will handle it in a future step.
/// 
/// ERROR HANDLING STRATEGY:
/// The service follows a "best effort" approach:
/// - Continue processing even if some rows fail
/// - Collect all errors and return them together
/// - Preserve original row data for debugging
/// - Provide detailed error messages
/// 
/// This allows users to see all problems at once and fix their CSV files
/// incrementally rather than fixing one error at a time.
/// 
/// PART 1 SCOPE:
/// This implementation (PART 1) focuses ONLY on parsing:
/// - Read CSV file
/// - Map columns to DTOs
/// - Handle encoding
/// - Collect parsing errors
/// 
/// Validation, database operations, and business logic will be added in future parts.
/// </summary>
public class WaybillImportService : IWaybillImportService
{
    private readonly ILogger<WaybillImportService> _logger;
    private readonly IWaybillValidationService _validationService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the WaybillImportService.
    /// </summary>
    /// <param name="logger">Logger for recording import operations and errors.</param>
    /// <param name="validationService">Service for validating waybill data.</param>
    /// <param name="dbContext">Database context for persisting waybill data.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="messagePublisher">Service for publishing events to RabbitMQ.</param>
    /// <param name="cacheService">Service for cache invalidation after import.</param>
    public WaybillImportService(
        ILogger<WaybillImportService> logger,
        IWaybillValidationService validationService,
        ApplicationDbContext dbContext,
        ITenantService tenantService,
        IMessagePublisher messagePublisher,
        ICacheService cacheService)
    {
        _logger = logger;
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Imports waybill data from a CSV stream.
    /// 
    /// This method parses the CSV file using CsvHelper, configured for UTF-8
    /// encoding and Hebrew text support. It processes each row, maps it to
    /// ImportWaybillDto, and collects any parsing errors.
    /// 
    /// PROCESSING STEPS:
    /// 1. Create StreamReader with UTF-8 encoding (handles BOM automatically)
    /// 2. Configure CsvHelper with UTF-8 and Hebrew support
    /// 3. Read header row and validate column mapping
    /// 4. Parse each data row into ImportWaybillDto
    /// 5. Collect errors for rows that fail to parse
    /// 6. Return comprehensive ImportResultDto
    /// 
    /// ENCODING HANDLING:
    /// The method explicitly uses UTF-8 encoding to ensure Hebrew characters
    /// are correctly read from the CSV file. StreamReader automatically handles
    /// UTF-8 BOM if present, and CsvHelper is configured to use UTF-8.
    /// 
    /// ERROR COLLECTION:
    /// If a row fails to parse (e.g., missing required field, invalid format),
    /// an ImportErrorDto is created with:
    /// - Row number (1-based, excluding header)
    /// - Field name where error occurred
    /// - Error message
    /// - Original row data for debugging
    /// 
    /// Processing continues for remaining rows, allowing partial imports.
    /// </summary>
    /// <param name="csvStream">The CSV file stream. Must be UTF-8 encoded.</param>
    /// <param name="tenantId">The tenant ID for the imported waybills.</param>
    /// <returns>ImportResultDto with parsed data and any errors.</returns>
    public async Task<ImportResultDto> ImportFromCsvAsync(Stream csvStream, string tenantId)
    {
            var result = new ImportResultDto();
            var parsedWaybills = new List<ImportWaybillDto>();
            var errors = new List<ImportErrorDto>();
            var warnings = new List<string>();
            
            // Track duplicate waybill IDs within this import
            // Key format: "{waybill_id}|{supplier_id}|{delivery_date}"
            var existingWaybillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Starting CSV import for tenant {TenantId}. Stream length: {StreamLength} bytes",
                tenantId,
                csvStream.Length);

        try
        {
            // ============================================================================
            // CSV HELPER CONFIGURATION FOR HEBREW TEXT SUPPORT
            // ============================================================================
            // Configure CsvHelper to properly handle UTF-8 encoded Hebrew text.
            // 
            // ENCODING SETUP:
            // - StreamReader with UTF-8 encoding (handles BOM automatically)
            // - CsvHelper configured to use UTF-8
            // - Proper handling of Hebrew Unicode characters (U+0590 to U+05FF)
            //
            // WHY UTF-8:
            // Hebrew characters require Unicode encoding. UTF-8 is the standard
            // encoding for CSV files with international characters. Without UTF-8,
            // Hebrew text will appear as question marks or corrupted characters.
            //
            // BOM HANDLING:
            // StreamReader automatically detects and handles UTF-8 BOM if present.
            // This is important because some CSV export tools include BOM, while
            // others don't. The code handles both cases correctly.

            // ============================================================================
            // DETECT DELIMITER (CSV vs TSV)
            // ============================================================================
            // Detect delimiter by reading the first line and counting tabs vs commas.
            // This allows the service to handle both CSV (comma-delimited) and TSV (tab-delimited) files.
            // The Gekko sample data uses tab-delimited format.
            var delimiter = ","; // Default to comma
            var position = csvStream.Position;
            
            using (var tempReader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: true))
            {
                var firstLine = await tempReader.ReadLineAsync();
                if (firstLine != null)
                {
                    // Count tabs and commas in the first line
                    var tabCount = firstLine.Count(c => c == '\t');
                    var commaCount = firstLine.Count(c => c == ',');
                    
                    // Use tab if there are more tabs than commas, otherwise use comma
                    if (tabCount > commaCount)
                    {
                        delimiter = "\t";
                        _logger.LogInformation("Detected tab-delimited file (TSV format)");
                    }
                    else
                    {
                        delimiter = ",";
                        _logger.LogInformation("Detected comma-delimited file (CSV format)");
                    }
                }
            }
            
            // Reset stream position to beginning for actual parsing
            csvStream.Position = position;

            using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: false);

            // ============================================================================
            // CSV CONFIGURATION
            // ============================================================================
            // Configure CsvHelper with settings optimized for waybill import:
            //
            // - HasHeaderRecord = true: First row contains column names
            // - HeaderValidated = null: Don't throw if extra columns exist (flexible)
            // - MissingFieldFound = null: Don't throw if columns are missing (handle in validation)
            // - TrimOptions = Trim: Remove whitespace from field values
            // - CultureInfo.InvariantCulture: Use invariant culture for number parsing
            //   (ensures consistent decimal parsing regardless of system locale)
            // - Delimiter: Auto-detected (comma or tab)
            //
            // HEBREW TEXT CONSIDERATIONS:
            // - Encoding is handled by StreamReader (UTF-8)
            // - CsvHelper automatically handles Unicode characters
            // - No special configuration needed for Hebrew - UTF-8 handles it natively

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // First row is header with column names
                HeaderValidated = null, // Don't throw if extra columns exist
                MissingFieldFound = null, // Don't throw if columns are missing (we'll validate)
                TrimOptions = TrimOptions.Trim, // Remove leading/trailing whitespace
                Encoding = Encoding.UTF8, // Explicitly set UTF-8 encoding
                Delimiter = delimiter, // Auto-detected: comma or tab
                Quote = '"', // Quote character for fields containing commas
                IgnoreBlankLines = true, // Skip empty rows
                BadDataFound = context => // Handle malformed rows
                {
                    // Log bad data for debugging
                    // The actual error will be caught in the parsing loop
                    _logger.LogWarning(
                        "Bad data found: {RawRecord}",
                        context.RawRecord ?? string.Empty);
                }
            };

            using var csv = new CsvReader(reader, csvConfig);

            // ============================================================================
            // REGISTER CLASS MAP
            // ============================================================================
            // Register the ImportWaybillDto class map to tell CsvHelper how to map
            // CSV columns to DTO properties. The [Name] attributes on ImportWaybillDto
            // properties define the column name mapping.
            //
            // This allows flexible CSV formats - columns can be in any order as long
            // as they have the correct names (defined by [Name] attributes).

            csv.Context.RegisterClassMap<ImportWaybillDtoMap>();

            // ============================================================================
            // PARSE AND VALIDATE CSV ROWS
            // ============================================================================
            // Read and parse each row from the CSV file. For each row:
            // 1. Attempt to parse into ImportWaybillDto
            // 2. If parsing succeeds, validate the data using WaybillValidationService
            // 3. If validation passes, add to parsedWaybills list
            // 4. If validation fails, collect errors and continue processing
            // 5. Collect business rule warnings for all rows
            // 6. Continue processing remaining rows (best effort strategy)
            //
            // VALIDATION INTEGRATION:
            // The validation service performs:
            // - Required field validation
            // - Data type validation (dates, decimals)
            // - Business rule validation
            // - Duplicate detection
            //
            // Only rows that pass all validation are added to ParsedWaybills.
            // Rows with validation errors are excluded but errors are reported.

            var rowNumber = 0; // Track row number (0 = header, 1+ = data rows)
            var dataRowCount = 0; // Track actual number of data rows processed

            await foreach (var record in csv.GetRecordsAsync<ImportWaybillDto>())
            {
                rowNumber = csv.Parser.Row; // Current row number (1-based, includes header)
                dataRowCount++; // Increment count of data rows processed
                var rowData = csv.Parser.RawRecord ?? string.Empty;

                try
                {
                    // Row parsed successfully - now validate it
                    var validationErrors = _validationService.ValidateWaybill(
                        record, 
                        rowNumber, 
                        rowData,
                        existingWaybillIds);

                    // CRITICAL SECURITY CHECK: Validate tenant_id match
                    // This ensures CSV tenant_id matches the header tenant_id to prevent data leakage
                    var tenantIdValidationErrors = _validationService.ValidateTenantIdMatch(
                        record.TenantId,
                        tenantId,
                        rowNumber,
                        rowData);
                    validationErrors.AddRange(tenantIdValidationErrors);

                    if (validationErrors.Any())
                    {
                        // Validation failed - add errors and continue
                        errors.AddRange(validationErrors);
                        result.ErrorCount++;
                        
                        _logger.LogWarning(
                            "Row {RowNumber} failed validation with {ErrorCount} errors",
                            rowNumber,
                            validationErrors.Count);
                    }
                    else
                    {
                        // Validation passed - add to list for database operations
                        parsedWaybills.Add(record);

                        // Check for business rule warnings (non-blocking)
                        var businessWarnings = _validationService.ValidateBusinessRules(record);
                        if (businessWarnings.Any())
                        {
                            warnings.AddRange(businessWarnings.Select(w => 
                                $"Row {rowNumber}: {w}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Unexpected error during validation - record and continue
                    _logger.LogError(
                        ex,
                        "Unexpected error validating row {RowNumber}",
                        rowNumber);

                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        Field = null,
                        Message = $"Unexpected error during validation: {ex.Message}",
                        RowData = rowData
                    });
                    result.ErrorCount++;
                }
            }

            // TotalRows should be the number of data rows processed (excluding header)
            // csv.Parser.Row is 1-based and includes the header row, so we use dataRowCount
            // which accurately counts only the data rows that were processed
            result.TotalRows = dataRowCount;
            result.Warnings = warnings;

            // ============================================================================
            // DATABASE OPERATIONS
            // ============================================================================
            // After parsing and validation, persist the validated waybills to the database.
            // This step uses transactions to ensure data consistency and allows rollback
            // if critical errors occur.
            //
            // UPSERT STRATEGY:
            // For each validated waybill:
            // 1. Check if waybill exists (by waybill_id + tenant_id)
            // 2. If exists: Update existing waybill (preserve CreatedAt, update other fields)
            // 3. If not exists: Create new waybill
            //
            // PROJECT AND SUPPLIER HANDLING:
            // Projects and Suppliers are created automatically if they don't exist for
            // the current tenant. This allows importing waybills even if the referenced
            // project or supplier hasn't been created yet.
            //
            // TRANSACTION MANAGEMENT:
            // All database operations are wrapped in a transaction to ensure:
            // - Atomicity: All waybills are saved or none are saved
            // - Consistency: Database remains in a valid state
            // - Rollback capability: Can undo all changes if critical error occurs
            //
            // ERROR RECOVERY:
            // Individual row errors don't rollback the entire transaction. Only critical
            // errors (e.g., database connection loss) will cause a rollback. This allows
            // partial imports where some rows succeed and others fail.

            if (parsedWaybills.Any())
            {
                _logger.LogInformation(
                    "Starting database operations for {Count} validated waybills for tenant {TenantId}",
                    parsedWaybills.Count,
                    tenantId);

                await SaveWaybillsToDatabaseAsync(parsedWaybills, tenantId, errors, result);
            }

            result.ParsedWaybills = parsedWaybills;
            result.Errors = errors;

            _logger.LogInformation(
                "CSV import completed for tenant {TenantId}. " +
                "Total rows: {TotalRows}, Success: {SuccessCount}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                tenantId,
                result.TotalRows,
                result.SuccessCount,
                result.ErrorCount,
                warnings.Count);

            // ============================================================================
            // CACHE INVALIDATION (BACKUP - if SaveWaybillsToDatabaseAsync didn't invalidate)
            // ============================================================================
            // This is a safety net in case SaveWaybillsToDatabaseAsync didn't invalidate
            // (e.g., if it was called but no waybills were saved). This ensures cache
            // is invalidated even if the database save path didn't handle it.
            //
            // NOTE: This is idempotent - calling RemoveByPatternAsync multiple times is safe.
            if (result.SuccessCount > 0)
            {
                try
                {
                    await _cacheService.RemoveByPatternAsync($"waybill:summary:{tenantId}:*");
                    await _cacheService.RemoveByPatternAsync($"supplier:summary:{tenantId}:*");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to invalidate cache for tenant {TenantId} after import completion. " +
                        "This is non-critical - cache will expire in 5 minutes.",
                        tenantId);
                }
            }

            // ============================================================================
            // PUBLISH EVENT TO RABBITMQ
            // ============================================================================
            // After successful import, publish an event to RabbitMQ to notify other
            // services about the import. This enables event-driven architecture where
            // other services can react to imports (e.g., update statistics, send notifications).
            //
            // EVENT-DRIVEN ARCHITECTURE:
            // Publishing events decouples the import service from other services that
            // need to react to imports. Instead of direct dependencies, services communicate
            // through events, making the system more flexible and scalable.
            //
            // ERROR HANDLING:
            // Event publishing is fire-and-forget. If RabbitMQ is unavailable, the error
            // is logged but does not affect the import operation. The import completes
            // successfully even if event publishing fails.
            if (result.SuccessCount > 0 || result.ErrorCount > 0)
            {
                try
                {
                    var importEvent = new WaybillsImportedEvent
                    {
                        TenantId = tenantId,
                        ImportedCount = result.TotalRows,
                        SuccessCount = result.SuccessCount,
                        ErrorCount = result.ErrorCount,
                        Timestamp = DateTime.UtcNow
                    };

                    await _messagePublisher.PublishWaybillsImportedEventAsync(importEvent);

                    _logger.LogInformation(
                        "Published waybill import event for tenant {TenantId}",
                        tenantId);
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - event publishing is fire-and-forget
                    // The import operation completed successfully, so we don't want to
                    // fail the entire operation just because event publishing failed
                    _logger.LogWarning(
                        ex,
                        "Failed to publish waybill import event for tenant {TenantId}. " +
                        "The import completed successfully, but the event was not published.",
                        tenantId);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Fatal error during CSV import for tenant {TenantId}",
                tenantId);

            // If a fatal error occurs (e.g., file is not a valid CSV), return
            // an error result with the exception message
            result.ErrorCount = result.TotalRows;
            result.Errors.Add(new ImportErrorDto
            {
                RowNumber = 0,
                Field = null,
                Message = $"Fatal error during CSV import: {ex.Message}",
                RowData = string.Empty
            });

            return result;
        }
    }

    /// <summary>
    /// Saves validated waybills to the database using upsert logic.
    /// 
    /// UPSERT STRATEGY:
    /// For each waybill:
    /// 1. Check if waybill exists (by waybill_id + tenant_id)
    /// 2. If exists: Update existing waybill (preserve CreatedAt, update other fields)
    /// 3. If not exists: Create new waybill with current timestamp
    /// 
    /// PROJECT AND SUPPLIER CREATION:
    /// Projects and Suppliers are created automatically if they don't exist for the tenant.
    /// This allows importing waybills even if referenced entities haven't been created yet.
    /// 
    /// TRANSACTION MANAGEMENT:
    /// All operations are wrapped in a database transaction to ensure atomicity.
    /// If a critical error occurs, the transaction is rolled back. Individual row errors
    /// are collected but don't cause rollback, allowing partial imports.
    /// 
    /// PERFORMANCE CONSIDERATIONS:
    /// For bulk imports, this method processes waybills one at a time to ensure proper
    /// error handling and logging. Future optimizations could include:
    /// - Batch inserts for new waybills
    /// - Bulk update operations
    /// - Caching of Projects and Suppliers
    /// </summary>
    /// <param name="parsedWaybills">List of validated waybill DTOs to save.</param>
    /// <param name="tenantId">The tenant ID for the waybills.</param>
    /// <param name="errors">List to collect database errors.</param>
    /// <param name="result">Import result to update with success/error counts.</param>
    private async Task SaveWaybillsToDatabaseAsync(
        List<ImportWaybillDto> parsedWaybills,
        string tenantId,
        List<ImportErrorDto> errors,
        ImportResultDto result)
    {
        // NOTE: Tenant ID is already set by TenantMiddleware and available through ITenantService.
        // The global query filters in ApplicationDbContext will automatically filter queries by tenant.
        // We verify the tenantId parameter matches the current tenant context for security.

        // Verify tenant ID matches current context (security check)
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (currentTenantId != tenantId)
        {
            throw new InvalidOperationException(
                $"Tenant ID mismatch. Expected {tenantId} but current context has {currentTenantId}");
        }

        // Ensure tenant exists first (required for foreign key constraints)
        await EnsureTenantExistsAsync(tenantId);

        // Use a database transaction to ensure atomicity
        // If a critical error occurs, all changes are rolled back
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            _logger.LogInformation(
                "Starting database transaction for {Count} waybills",
                parsedWaybills.Count);

            // Track created/updated projects and suppliers to avoid duplicate queries
            var projectCache = new Dictionary<string, Project>();
            var supplierCache = new Dictionary<string, Supplier>();

            // Process each validated waybill
            foreach (var dto in parsedWaybills)
            {
                try
                {
                    // Ensure Project exists (create if not exists)
                    // Use ProjectName from CSV if available, otherwise use ProjectId as fallback
                    var projectName = !string.IsNullOrWhiteSpace(dto.ProjectName) 
                        ? dto.ProjectName 
                        : dto.ProjectId; // Fallback to ID if name not provided
                    
                    var project = await EnsureProjectExistsAsync(
                        dto.ProjectId,
                        projectName, // Use ProjectName from CSV
                        tenantId,
                        projectCache);

                    // Ensure Supplier exists (create if not exists)
                    // Use SupplierName from CSV if available, otherwise use SupplierId as fallback
                    var supplierName = !string.IsNullOrWhiteSpace(dto.SupplierName) 
                        ? dto.SupplierName 
                        : dto.SupplierId; // Fallback to ID if name not provided
                    
                    var supplier = await EnsureSupplierExistsAsync(
                        dto.SupplierId,
                        supplierName, // Use SupplierName from CSV
                        tenantId,
                        supplierCache);

                    // Parse dates and decimals
                    var waybillDate = ParseDate(dto.WaybillDate);
                    var deliveryDate = ParseDate(dto.DeliveryDate);
                    var quantity = decimal.Parse(dto.Quantity, CultureInfo.InvariantCulture);
                    var unitPrice = decimal.Parse(dto.UnitPrice, CultureInfo.InvariantCulture);
                    var totalAmount = decimal.Parse(dto.TotalAmount, CultureInfo.InvariantCulture);

                    // Parse status (default to Pending if not provided)
                    var status = string.IsNullOrWhiteSpace(dto.Status)
                        ? WaybillStatus.Pending
                        : Enum.Parse<WaybillStatus>(dto.Status.Trim(), ignoreCase: true);

                    // Parse CreatedAt from CSV if provided, otherwise use current time
                    DateTime createdAt;
                    if (!string.IsNullOrWhiteSpace(dto.CreatedAt))
                    {
                        // Try to parse the CreatedAt from CSV (ISO 8601 format: "2024-09-01T08:30:00Z")
                        if (DateTime.TryParse(dto.CreatedAt, CultureInfo.InvariantCulture, 
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedCreatedAt))
                        {
                            createdAt = parsedCreatedAt;
                        }
                        else
                        {
                            // If parsing fails, log warning and use current time
                            _logger.LogWarning(
                                "Failed to parse CreatedAt '{CreatedAt}' for waybill {WaybillId}. Using current time.",
                                dto.CreatedAt,
                                dto.WaybillId);
                            createdAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // No CreatedAt in CSV, use current time
                        createdAt = DateTime.UtcNow;
                    }

                    // Check if waybill already exists
                    var existingWaybill = await _dbContext.Waybills
                        .FirstOrDefaultAsync(w => w.Id == dto.WaybillId && w.TenantId == tenantId);

                    if (existingWaybill != null)
                    {
                        // UPDATE EXISTING WAYBILL (Upsert - Update path)
                        // Preserve CreatedAt, update all other fields
                        _logger.LogDebug(
                            "Updating existing waybill {WaybillId} for tenant {TenantId}",
                            dto.WaybillId,
                            tenantId);

                        existingWaybill.WaybillDate = waybillDate;
                        existingWaybill.DeliveryDate = deliveryDate;
                        existingWaybill.ProjectId = dto.ProjectId;
                        existingWaybill.SupplierId = dto.SupplierId;
                        existingWaybill.ProductCode = dto.ProductCode;
                        existingWaybill.ProductName = dto.ProductName;
                        existingWaybill.Quantity = quantity;
                        existingWaybill.Unit = dto.Unit;
                        existingWaybill.UnitPrice = unitPrice;
                        existingWaybill.TotalAmount = totalAmount;
                        existingWaybill.Currency = dto.Currency ?? "ILS";
                        existingWaybill.Status = status;
                        existingWaybill.VehicleNumber = dto.VehicleNumber;
                        existingWaybill.DriverName = dto.DriverName;
                        existingWaybill.DeliveryAddress = dto.DeliveryAddress;
                        existingWaybill.Notes = dto.Notes;
                        existingWaybill.UpdatedAt = DateTime.UtcNow;

                        _dbContext.Waybills.Update(existingWaybill);
                    }
                    else
                    {
                        // CREATE NEW WAYBILL (Upsert - Insert path)
                        _logger.LogDebug(
                            "Creating new waybill {WaybillId} for tenant {TenantId}",
                            dto.WaybillId,
                            tenantId);

                        var newWaybill = new Waybill
                        {
                            Id = dto.WaybillId,
                            WaybillDate = waybillDate,
                            DeliveryDate = deliveryDate,
                            ProjectId = dto.ProjectId,
                            SupplierId = dto.SupplierId,
                            ProductCode = dto.ProductCode,
                            ProductName = dto.ProductName,
                            Quantity = quantity,
                            Unit = dto.Unit,
                            UnitPrice = unitPrice,
                            TotalAmount = totalAmount,
                            Currency = dto.Currency ?? "ILS",
                            Status = status,
                            VehicleNumber = dto.VehicleNumber,
                            DriverName = dto.DriverName,
                            DeliveryAddress = dto.DeliveryAddress,
                            Notes = dto.Notes,
                            TenantId = tenantId,
                            CreatedAt = createdAt // Use parsed CreatedAt from CSV
                        };

                        await _dbContext.Waybills.AddAsync(newWaybill);
                    }

                    result.SuccessCount++;
                }
                catch (DbUpdateException dbEx)
                {
                    // Database constraint violation or foreign key issue
                    _logger.LogError(
                        dbEx,
                        "Database error saving waybill {WaybillId} for tenant {TenantId}",
                        dto.WaybillId,
                        tenantId);

                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = 0, // Row number not available in this context
                        Field = null,
                        Message = $"Database error saving waybill '{dto.WaybillId}': {dbEx.InnerException?.Message ?? dbEx.Message}",
                        RowData = $"waybill_id: {dto.WaybillId}"
                    });
                    result.ErrorCount++;
                    result.SuccessCount--; // Adjust success count since this failed
                }
                catch (Exception ex)
                {
                    // Other errors (parsing, validation, etc.)
                    _logger.LogError(
                        ex,
                        "Error processing waybill {WaybillId} for tenant {TenantId}",
                        dto.WaybillId,
                        tenantId);

                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = 0,
                        Field = null,
                        Message = $"Error processing waybill '{dto.WaybillId}': {ex.Message}",
                        RowData = $"waybill_id: {dto.WaybillId}"
                    });
                    result.ErrorCount++;
                    result.SuccessCount--; // Adjust success count since this failed
                }
            }

            // Save all changes to database
            var savedCount = await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Saved {SavedCount} waybills to database for tenant {TenantId}",
                savedCount,
                tenantId);

            // Commit transaction if all operations succeeded
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Database transaction committed successfully for tenant {TenantId}",
                tenantId);

            // ============================================================================
            // CACHE INVALIDATION
            // ============================================================================
            // After successful import, invalidate cached summaries to ensure fresh data
            // This ensures that GET /api/waybills/summary returns updated statistics
            // immediately after import, rather than waiting for cache expiration (5 minutes)
            //
            // INVALIDATION STRATEGY:
            // - Invalidate all waybill summaries for the tenant (all date ranges)
            // - Invalidate all supplier summaries for the tenant (all suppliers)
            // - Pattern-based invalidation ensures all related caches are cleared
            //
            // ERROR HANDLING:
            // Cache invalidation failures are logged but don't affect the import operation.
            // If cache invalidation fails, the worst case is stale cache data (which expires
            // in 5 minutes anyway). The import operation succeeds regardless.
            if (result.SuccessCount > 0)
            {
                try
                {
                    _logger.LogDebug(
                        "Invalidating cache for tenant {TenantId} after successful import of {SuccessCount} waybills",
                        tenantId,
                        result.SuccessCount);

                    // Invalidate waybill summaries (all date ranges)
                    await _cacheService.RemoveByPatternAsync($"waybill:summary:{tenantId}:*");
                    
                    // Invalidate supplier summaries (all suppliers)
                    await _cacheService.RemoveByPatternAsync($"supplier:summary:{tenantId}:*");

                    _logger.LogDebug(
                        "Cache invalidated successfully for tenant {TenantId}",
                        tenantId);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the import operation
                    // Cache will expire naturally in 5 minutes, so this is not critical
                    _logger.LogWarning(
                        ex,
                        "Failed to invalidate cache for tenant {TenantId} after import. " +
                        "Cache will expire naturally in 5 minutes. Import operation succeeded.",
                        tenantId);
                }
            }
        }
        catch (Exception ex)
        {
            // Critical error - rollback transaction
            _logger.LogError(
                ex,
                "Critical error during database operations. Rolling back transaction for tenant {TenantId}",
                tenantId);

            await transaction.RollbackAsync();

            // Add error to result
            errors.Add(new ImportErrorDto
            {
                RowNumber = 0,
                Field = null,
                Message = $"Critical database error: {ex.Message}. All changes have been rolled back.",
                RowData = string.Empty
            });

            // Reset success count since transaction was rolled back
            result.SuccessCount = 0;
            result.ErrorCount = parsedWaybills.Count;
        }
    }

    /// <summary>
    /// Ensures a Project exists for the tenant, creating it if necessary.
    /// 
    /// This method checks if a project with the given ID exists for the tenant.
    /// If it doesn't exist, it creates a new project. This allows importing waybills
    /// even if the referenced project hasn't been created yet.
    /// 
    /// CACHING:
    /// Projects are cached in a dictionary to avoid duplicate database queries
    /// during the same import operation.
    /// </summary>
    /// <param name="projectId">The project ID to check/create.</param>
    /// <param name="projectName">The project name (used if creating new project).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cache">Cache dictionary to avoid duplicate queries.</param>
    /// <returns>The Project entity (existing or newly created).</returns>
    private async Task<Project> EnsureProjectExistsAsync(
        string projectId,
        string projectName,
        string tenantId,
        Dictionary<string, Project> cache)
    {
        // Check cache first
        if (cache.TryGetValue(projectId, out var cachedProject))
        {
            return cachedProject;
        }

        // Check database (use AsNoTracking to avoid tracking conflicts)
        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId);

        if (project == null)
        {
            // Create new project
            _logger.LogInformation(
                "Creating new project {ProjectId} for tenant {TenantId}",
                projectId,
                tenantId);

            project = new Project
            {
                Id = projectId,
                Name = projectName,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync(); // Save immediately to ensure it exists for foreign key
            
            // Detach the entity to avoid tracking conflicts when used in multiple waybills
            _dbContext.Entry(project).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            _logger.LogInformation(
                "Created project {ProjectId} for tenant {TenantId}",
                projectId,
                tenantId);
        }
        else
        {
            // If project exists, attach it to the context for use in waybills
            // But only if it's not already tracked
            if (_dbContext.Entry(project).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                _dbContext.Projects.Attach(project);
            }
        }

        // Add to cache
        cache[projectId] = project;
        return project;
    }

    /// <summary>
    /// Ensures a Supplier exists for the tenant, creating it if necessary.
    /// 
    /// This method checks if a supplier with the given ID exists for the tenant.
    /// If it doesn't exist, it creates a new supplier. This allows importing waybills
    /// even if the referenced supplier hasn't been created yet.
    /// 
    /// CACHING:
    /// Suppliers are cached in a dictionary to avoid duplicate database queries
    /// during the same import operation.
    /// </summary>
    /// <param name="supplierId">The supplier ID to check/create.</param>
    /// <param name="supplierName">The supplier name (used if creating new supplier).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cache">Cache dictionary to avoid duplicate queries.</param>
    /// <returns>The Supplier entity (existing or newly created).</returns>
    private async Task<Supplier> EnsureSupplierExistsAsync(
        string supplierId,
        string supplierName,
        string tenantId,
        Dictionary<string, Supplier> cache)
    {
        // Check cache first
        if (cache.TryGetValue(supplierId, out var cachedSupplier))
        {
            return cachedSupplier;
        }

        // Check database (use AsNoTracking to avoid tracking conflicts)
        var supplier = await _dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.TenantId == tenantId);

        if (supplier == null)
        {
            // Create new supplier
            _logger.LogInformation(
                "Creating new supplier {SupplierId} for tenant {TenantId}",
                supplierId,
                tenantId);

            supplier = new Supplier
            {
                Id = supplierId,
                Name = supplierName,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Suppliers.AddAsync(supplier);
            await _dbContext.SaveChangesAsync(); // Save immediately to ensure it exists for foreign key
            
            // Detach the entity to avoid tracking conflicts when used in multiple waybills
            _dbContext.Entry(supplier).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            _logger.LogInformation(
                "Created supplier {SupplierId} for tenant {TenantId}",
                supplierId,
                tenantId);
        }
        else
        {
            // If supplier exists, attach it to the context for use in waybills
            // But only if it's not already tracked
            if (_dbContext.Entry(supplier).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                _dbContext.Suppliers.Attach(supplier);
            }
        }

        // Add to cache
        cache[supplierId] = supplier;
        return supplier;
    }

    /// <summary>
    /// Ensures a Tenant exists in the database, creating it if necessary.
    /// 
    /// This method is called before importing waybills to ensure the tenant
    /// exists, which is required for foreign key constraints on Projects and Suppliers.
    /// </summary>
    /// <param name="tenantId">The tenant ID to check/create.</param>
    private async Task EnsureTenantExistsAsync(string tenantId)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
        {
            _logger.LogInformation(
                "Creating new tenant {TenantId}",
                tenantId);

            tenant = new Tenant
            {
                Id = tenantId,
                Name = $"Tenant {tenantId}", // Default name
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Tenants.AddAsync(tenant);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Created tenant {TenantId}",
                tenantId);
        }
    }

    /// <summary>
    /// Parses a date string using multiple common formats.
    /// </summary>
    /// <param name="dateString">The date string to parse.</param>
    /// <returns>The parsed DateTime.</returns>
    /// <exception cref="FormatException">Thrown if date cannot be parsed.</exception>
    private DateTime ParseDate(string dateString)
    {
        var formats = new[]
        {
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "yyyy/MM/dd",
            "dd-MM-yyyy",
        };

        if (DateTime.TryParseExact(
            dateString.Trim(),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date))
        {
            return date;
        }

        throw new FormatException($"Unable to parse date: {dateString}");
    }

    /// <summary>
    /// Class map for ImportWaybillDto to configure CsvHelper column mapping.
    /// 
    /// This class map tells CsvHelper how to map CSV columns to ImportWaybillDto
    /// properties. The mapping is defined by [Name] attributes on the DTO properties,
    /// but this class map allows for additional configuration if needed.
    /// 
    /// CURRENT IMPLEMENTATION:
    /// The class map uses automatic mapping based on [Name] attributes. This means
    /// CsvHelper will automatically match CSV column names to DTO properties based
    /// on the [Name] attribute values.
    /// 
    /// FUTURE ENHANCEMENTS:
    /// This class map can be extended to:
    /// - Handle column name variations (e.g., "waybill_id" vs "waybillId")
    /// - Provide default values for missing columns
    /// - Custom type converters for specific fields
    /// - Handle different date formats
    /// </summary>
    private sealed class ImportWaybillDtoMap : ClassMap<ImportWaybillDto>
    {
        public ImportWaybillDtoMap()
        {
            // Use automatic mapping based on [Name] attributes
            // This allows flexible CSV formats - columns can be in any order
            AutoMap(CultureInfo.InvariantCulture);

            // All property mappings are defined by [Name] attributes on ImportWaybillDto
            // No additional configuration needed at this stage
        }
    }
}
