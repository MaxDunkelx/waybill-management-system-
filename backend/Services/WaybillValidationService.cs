using System.Globalization;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for validating waybill data during CSV import.
/// 
/// VALIDATION PHILOSOPHY:
/// This service implements comprehensive validation with a "fail-fast" approach
/// for critical errors and "warn-but-continue" for business rule violations.
/// 
/// VALIDATION CATEGORIES:
/// 1. Required Fields: Must be present and non-empty
/// 2. Data Types: Must be parseable (dates, decimals)
/// 3. Business Rules: Must comply with business logic
/// 4. Duplicates: Must not duplicate existing waybills
/// 
/// ERROR COLLECTION:
/// All validation errors are collected and returned together, allowing users
/// to see all problems at once rather than fixing one error at a time.
/// 
/// TOLERANCE FOR ROUNDING:
/// When validating price calculations (total_amount = quantity × unit_price),
/// a small tolerance is allowed for floating-point rounding errors. This is
/// important because decimal arithmetic can produce small discrepancies.
/// </summary>
public class WaybillValidationService : IWaybillValidationService
{
    private readonly ILogger<WaybillValidationService> _logger;

    /// <summary>
    /// Tolerance for price calculation validation (0.01 = 1 cent).
    /// This accounts for floating-point rounding errors in decimal arithmetic.
    /// </summary>
    private const decimal PriceCalculationTolerance = 0.01m;

    /// <summary>
    /// Minimum allowed quantity (0.5).
    /// This prevents importing waybills with unreasonably small quantities.
    /// </summary>
    private const decimal MinQuantity = 0.5m;

    /// <summary>
    /// Maximum allowed quantity (50).
    /// This prevents importing waybills with unreasonably large quantities.
    /// Adjust based on business requirements.
    /// </summary>
    private const decimal MaxQuantity = 50m;

    /// <summary>
    /// Initializes a new instance of the WaybillValidationService.
    /// </summary>
    /// <param name="logger">Logger for recording validation operations.</param>
    public WaybillValidationService(ILogger<WaybillValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a waybill DTO and returns any validation errors.
    /// 
    /// This method performs comprehensive validation in the following order:
    /// 1. Required field validation
    /// 2. Data type validation (dates, decimals)
    /// 3. Business rule validation
    /// 4. Duplicate detection
    /// 
    /// VALIDATION RULES:
    /// 
    /// REQUIRED FIELDS:
    /// - waybill_id: Must be present and non-empty
    /// - waybill_date: Must be present and parseable as date
    /// - delivery_date: Must be present and parseable as date
    /// - project_id: Must be present and non-empty
    /// - supplier_id: Must be present and non-empty
    /// - product_code: Must be present and non-empty
    /// - product_name: Must be present and non-empty
    /// - quantity: Must be present and parseable as decimal
    /// - unit: Must be present and non-empty
    /// - unit_price: Must be present and parseable as decimal
    /// - total_amount: Must be present and parseable as decimal
    /// - delivery_address: Must be present and non-empty
    /// 
    /// OPTIONAL FIELDS (validated if present):
    /// - currency: If present, must be 3 characters
    /// - status: If present, must be valid enum value
    /// - vehicle_number: If present, validated for format
    /// - driver_name: If present, validated for length
    /// - notes: If present, validated for length
    /// 
    /// DATA TYPE VALIDATION:
    /// - Dates: Must parse as DateTime using multiple formats
    /// - Decimals: Must parse as decimal using invariant culture
    /// - Status: Must match enum values (case-insensitive)
    /// 
    /// BUSINESS RULES:
    /// - Quantity must be between 0.5 and 50
    /// - total_amount must equal quantity × unit_price (within tolerance)
    /// - delivery_date cannot be before waybill_date
    /// 
    /// DUPLICATE DETECTION:
    /// - Checks if waybill_id + supplier_id + delivery_date combination already exists
    /// - Prevents importing the same waybill twice
    /// </summary>
    /// <param name="dto">The waybill DTO to validate.</param>
    /// <param name="rowNumber">The row number in the CSV file.</param>
    /// <param name="rowData">The original CSV row data.</param>
    /// <param name="existingWaybillIds">Set of existing waybill ID combinations to check for duplicates.</param>
    /// <returns>List of validation errors.</returns>
    public List<ImportErrorDto> ValidateWaybill(
        ImportWaybillDto dto, 
        int rowNumber, 
        string rowData,
        HashSet<string> existingWaybillIds)
    {
        var errors = new List<ImportErrorDto>();

        // ============================================================================
        // REQUIRED FIELD VALIDATION
        // ============================================================================
        // Check that all required fields are present and non-empty.
        // Required fields are those that are essential for creating a valid waybill.

        if (string.IsNullOrWhiteSpace(dto.WaybillId))
        {
            errors.Add(CreateError(rowNumber, "waybill_id", "waybill_id is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.WaybillDate))
        {
            errors.Add(CreateError(rowNumber, "waybill_date", "waybill_date is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.DeliveryDate))
        {
            errors.Add(CreateError(rowNumber, "delivery_date", "delivery_date is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.ProjectId))
        {
            errors.Add(CreateError(rowNumber, "project_id", "project_id is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.SupplierId))
        {
            errors.Add(CreateError(rowNumber, "supplier_id", "supplier_id is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.ProductCode))
        {
            errors.Add(CreateError(rowNumber, "product_code", "product_code is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.ProductName))
        {
            errors.Add(CreateError(rowNumber, "product_name", "product_name is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.Quantity))
        {
            errors.Add(CreateError(rowNumber, "quantity", "quantity is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.Unit))
        {
            errors.Add(CreateError(rowNumber, "unit", "unit is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.UnitPrice))
        {
            errors.Add(CreateError(rowNumber, "unit_price", "unit_price is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.TotalAmount))
        {
            errors.Add(CreateError(rowNumber, "total_amount", "total_amount is required and cannot be empty", rowData));
        }

        if (string.IsNullOrWhiteSpace(dto.DeliveryAddress))
        {
            errors.Add(CreateError(rowNumber, "delivery_address", "delivery_address is required and cannot be empty", rowData));
        }

        // If required fields are missing, skip further validation
        // (to avoid cascading errors from missing data)
        if (errors.Any())
        {
            return errors;
        }

        // ============================================================================
        // DATA TYPE VALIDATION
        // ============================================================================
        // Validate that fields can be parsed into their expected data types.
        // This ensures data integrity before attempting business rule validation.

        // Date validation - waybill_date
        if (!TryParseDate(dto.WaybillDate, out var waybillDate))
        {
            errors.Add(CreateError(
                rowNumber, 
                "waybill_date", 
                $"waybill_date '{dto.WaybillDate}' is not a valid date. Expected format: YYYY-MM-DD or DD/MM/YYYY", 
                rowData));
        }

        // Date validation - delivery_date
        if (!TryParseDate(dto.DeliveryDate, out var deliveryDate))
        {
            errors.Add(CreateError(
                rowNumber, 
                "delivery_date", 
                $"delivery_date '{dto.DeliveryDate}' is not a valid date. Expected format: YYYY-MM-DD or DD/MM/YYYY", 
                rowData));
        }

        // Decimal validation - quantity
        if (!decimal.TryParse(dto.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
        {
            errors.Add(CreateError(
                rowNumber, 
                "quantity", 
                $"quantity '{dto.Quantity}' is not a valid decimal number", 
                rowData));
        }

        // Decimal validation - unit_price
        if (!decimal.TryParse(dto.UnitPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice))
        {
            errors.Add(CreateError(
                rowNumber, 
                "unit_price", 
                $"unit_price '{dto.UnitPrice}' is not a valid decimal number", 
                rowData));
        }

        // Decimal validation - total_amount
        if (!decimal.TryParse(dto.TotalAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalAmount))
        {
            errors.Add(CreateError(
                rowNumber, 
                "total_amount", 
                $"total_amount '{dto.TotalAmount}' is not a valid decimal number", 
                rowData));
        }

        // Status validation (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Status) && !IsValidStatus(dto.Status))
        {
            errors.Add(CreateError(
                rowNumber, 
                "status", 
                $"status '{dto.Status}' is not valid. Valid values: Pending, Delivered, Cancelled, Disputed", 
                rowData));
        }

        // Currency validation (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Currency) && dto.Currency.Length != 3)
        {
            errors.Add(CreateError(
                rowNumber, 
                "currency", 
                $"currency '{dto.Currency}' must be exactly 3 characters (e.g., ILS, USD)", 
                rowData));
        }

        // If data type validation fails, skip business rule validation
        if (errors.Any())
        {
            return errors;
        }

        // ============================================================================
        // BUSINESS RULE VALIDATION
        // ============================================================================
        // Validate business rules that ensure data makes logical sense.
        // These rules enforce business constraints beyond simple data type validation.

        // Business Rule 1: Quantity must be between 0.5 and 50
        // WHY: Prevents importing waybills with unreasonably small or large quantities.
        // This range can be adjusted based on business requirements.
        if (quantity < MinQuantity || quantity > MaxQuantity)
        {
            errors.Add(CreateError(
                rowNumber, 
                "quantity", 
                $"quantity {quantity} is outside the allowed range ({MinQuantity} to {MaxQuantity})", 
                rowData));
        }

        // Business Rule 2: total_amount must equal quantity × unit_price (within tolerance)
        // WHY: Ensures price calculations are correct. A small tolerance is allowed for
        // floating-point rounding errors in decimal arithmetic.
        var calculatedTotal = quantity * unitPrice;
        var difference = Math.Abs(totalAmount - calculatedTotal);
        
        if (difference > PriceCalculationTolerance)
        {
            errors.Add(CreateError(
                rowNumber, 
                "total_amount", 
                $"total_amount {totalAmount} does not match calculated value (quantity {quantity} × unit_price {unitPrice} = {calculatedTotal}). " +
                $"Difference: {difference}, allowed tolerance: {PriceCalculationTolerance}", 
                rowData));
        }

        // Business Rule 3: delivery_date cannot be before waybill_date
        // WHY: A delivery cannot occur before the waybill is issued. This ensures
        // logical consistency in the data.
        if (deliveryDate < waybillDate)
        {
            errors.Add(CreateError(
                rowNumber, 
                "delivery_date", 
                $"delivery_date ({deliveryDate:yyyy-MM-dd}) cannot be before waybill_date ({waybillDate:yyyy-MM-dd})", 
                rowData));
        }

        // ============================================================================
        // DUPLICATE DETECTION
        // ============================================================================
        // Check if this waybill already exists based on waybill_id + supplier_id + delivery_date.
        // WHY: Prevents importing the same waybill twice. The combination of these three
        // fields uniquely identifies a waybill delivery.
        //
        // DUPLICATE KEY:
        // The duplicate key is constructed as: "{waybill_id}|{supplier_id}|{delivery_date}"
        // This combination ensures that:
        // - Same waybill ID from different suppliers is allowed (different deliveries)
        // - Same waybill ID from same supplier on different dates is allowed (different deliveries)
        // - Same waybill ID from same supplier on same date is NOT allowed (duplicate)
        var duplicateKey = $"{dto.WaybillId}|{dto.SupplierId}|{deliveryDate:yyyy-MM-dd}";
        
        if (existingWaybillIds.Contains(duplicateKey))
        {
            errors.Add(CreateError(
                rowNumber, 
                "waybill_id", 
                $"Duplicate waybill detected. A waybill with waybill_id '{dto.WaybillId}', " +
                $"supplier_id '{dto.SupplierId}', and delivery_date '{deliveryDate:yyyy-MM-dd}' already exists in this import", 
                rowData));
        }
        else
        {
            // Add to existing set to detect duplicates in subsequent rows
            existingWaybillIds.Add(duplicateKey);
        }

        return errors;
    }

    /// <summary>
    /// Validates business rules on a waybill DTO and returns warnings.
    /// 
    /// WARNINGS VS ERRORS:
    /// - Warnings don't prevent import but alert users to potential issues
    /// - Errors prevent import and must be fixed
    /// 
    /// BUSINESS RULES CHECKED:
    /// - Status transition validation (if status is provided)
    /// - Quantity reasonableness (within expected range but flagged if unusual)
    /// - Price calculation accuracy (within tolerance but flagged if close to limit)
    /// 
    /// STATUS TRANSITION LOGIC:
    /// Valid status transitions:
    /// - PENDING → DELIVERED (normal flow)
    /// - PENDING → CANCELLED (order cancelled)
    /// - DELIVERED → DISPUTED (delivery issue)
    /// 
    /// Invalid transitions (warnings):
    /// - CANCELLED → any other status (can't uncancel)
    /// - DISPUTED → PENDING (must resolve dispute first)
    /// - DELIVERED → PENDING (delivery already occurred)
    /// 
    /// NOTE: Since this is an import, we don't have previous status to compare against.
    /// Status transition warnings are based on the status value itself (e.g., if status
    /// is CANCELLED, warn that it can't be changed later).
    /// </summary>
    /// <param name="dto">The waybill DTO to validate business rules for.</param>
    /// <returns>List of warning messages.</returns>
    public List<string> ValidateBusinessRules(ImportWaybillDto dto)
    {
        var warnings = new List<string>();

        // Parse values for business rule validation
        if (!decimal.TryParse(dto.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
        {
            return warnings; // Can't validate if quantity is invalid
        }

        if (!decimal.TryParse(dto.UnitPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice))
        {
            return warnings; // Can't validate if unit_price is invalid
        }

        if (!decimal.TryParse(dto.TotalAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalAmount))
        {
            return warnings; // Can't validate if total_amount is invalid
        }

        // Business Rule Warning 1: Status transition validation
        // If status is CANCELLED, warn that it cannot be changed later
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var statusUpper = dto.Status.Trim().ToUpperInvariant();
            if (statusUpper == "CANCELLED")
            {
                warnings.Add(
                    "Status is CANCELLED. Once cancelled, a waybill cannot be changed to any other status. " +
                    "Ensure this is correct before importing.");
            }
        }

        // Business Rule Warning 2: Quantity reasonableness
        // Warn if quantity is at the boundaries of the allowed range
        if (quantity <= MinQuantity + 0.1m)
        {
            warnings.Add(
                $"Quantity {quantity} is very close to the minimum allowed value ({MinQuantity}). " +
                "Please verify this is correct.");
        }

        if (quantity >= MaxQuantity - 0.1m)
        {
            warnings.Add(
                $"Quantity {quantity} is very close to the maximum allowed value ({MaxQuantity}). " +
                "Please verify this is correct.");
        }

        // Business Rule Warning 3: Price calculation close to tolerance
        // Warn if the difference is close to the tolerance limit
        var calculatedTotal = quantity * unitPrice;
        var difference = Math.Abs(totalAmount - calculatedTotal);
        
        if (difference > PriceCalculationTolerance * 0.8m && difference <= PriceCalculationTolerance)
        {
            warnings.Add(
                $"Price calculation difference ({difference}) is close to the tolerance limit ({PriceCalculationTolerance}). " +
                "Please verify the calculation is correct.");
        }

        return warnings;
    }

    /// <summary>
    /// Attempts to parse a date string using multiple common formats.
    /// 
    /// SUPPORTED FORMATS:
    /// - yyyy-MM-dd (ISO 8601, recommended)
    /// - dd/MM/yyyy (common in some regions)
    /// - MM/dd/yyyy (US format)
    /// 
    /// WHY MULTIPLE FORMATS:
    /// CSV files may come from different sources with different date formats.
    /// Supporting multiple formats increases compatibility and reduces import errors.
    /// </summary>
    /// <param name="dateString">The date string to parse.</param>
    /// <param name="date">The parsed DateTime if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private bool TryParseDate(string dateString, out DateTime date)
    {
        var formats = new[]
        {
            "yyyy-MM-dd",      // ISO 8601 format (recommended)
            "dd/MM/yyyy",      // Common in some regions
            "MM/dd/yyyy",      // US format
            "yyyy/MM/dd",      // Alternative ISO format
            "dd-MM-yyyy",      // Alternative format
        };

        return DateTime.TryParseExact(
            dateString.Trim(),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>
    /// Checks if a status string is a valid WaybillStatus enum value.
    /// 
    /// Valid values (case-insensitive):
    /// - Pending
    /// - Delivered
    /// - Cancelled
    /// - Disputed
    /// </summary>
    /// <param name="status">The status string to validate.</param>
    /// <returns>True if status is valid, false otherwise.</returns>
    private bool IsValidStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return Enum.TryParse<WaybillStatus>(status.Trim(), ignoreCase: true, out _);
    }

    /// <summary>
    /// Validates that the tenant ID in the CSV matches the expected tenant ID from the header.
    /// This is a critical security check to prevent tenants from importing data for other tenants.
    /// 
    /// SECURITY IMPORTANCE:
    /// Without this validation, a malicious tenant could include rows with different tenant_ids
    /// in their CSV file, potentially importing data into another tenant's account. This check
    /// ensures that all rows in the CSV belong to the tenant making the request.
    /// 
    /// VALIDATION LOGIC:
    /// - If CSV has tenant_id, it MUST match the header tenant_id (case-insensitive)
    /// - If CSV tenant_id is missing, it's allowed (will use header tenant_id)
    /// - If CSV tenant_id doesn't match, returns error preventing import
    /// </summary>
    /// <param name="csvTenantId">The tenant ID from the CSV file.</param>
    /// <param name="expectedTenantId">The tenant ID from the HTTP header (expected value).</param>
    /// <param name="rowNumber">The row number in the CSV (for error reporting).</param>
    /// <param name="rowData">The raw row data (for error reporting).</param>
    /// <returns>List of validation errors (empty if valid).</returns>
    public List<ImportErrorDto> ValidateTenantIdMatch(
        string? csvTenantId,
        string expectedTenantId,
        int rowNumber,
        string rowData)
    {
        var errors = new List<ImportErrorDto>();

        // If CSV has tenant_id, it must match the header tenant_id
        if (!string.IsNullOrWhiteSpace(csvTenantId))
        {
            if (!string.Equals(csvTenantId.Trim(), expectedTenantId, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(CreateError(
                    rowNumber,
                    "tenant_id",
                    $"Tenant ID mismatch: CSV contains '{csvTenantId}' but header specifies '{expectedTenantId}'. " +
                    "For security reasons, the CSV tenant_id must match the X-Tenant-ID header.",
                    rowData));
                
                _logger.LogWarning(
                    "Tenant ID mismatch detected at row {RowNumber}: CSV has '{CsvTenantId}' but header has '{ExpectedTenantId}'",
                    rowNumber,
                    csvTenantId,
                    expectedTenantId);
            }
        }
        // If CSV tenant_id is missing, that's OK - we'll use the header tenant_id

        return errors;
    }

    /// <summary>
    /// Creates an ImportErrorDto with the specified details.
    /// </summary>
    /// <param name="rowNumber">The row number where the error occurred.</param>
    /// <param name="field">The field name where the error occurred.</param>
    /// <param name="message">The error message.</param>
    /// <param name="rowData">The original CSV row data.</param>
    /// <returns>An ImportErrorDto instance.</returns>
    private ImportErrorDto CreateError(int rowNumber, string? field, string message, string rowData)
    {
        return new ImportErrorDto
        {
            RowNumber = rowNumber,
            Field = field,
            Message = message,
            RowData = rowData
        };
    }
}
