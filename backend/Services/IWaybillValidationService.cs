using WaybillManagementSystem.DTOs;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for validating waybill data during CSV import.
/// 
/// PURPOSE:
/// This service provides comprehensive validation of waybill data, including:
/// - Required field validation
/// - Data type and format validation
/// - Business rule validation
/// - Duplicate detection
/// 
/// VALIDATION STRATEGY:
/// The service separates validation into two phases:
/// 1. ValidateWaybill: Validates individual waybill DTO and returns errors
/// 2. ValidateBusinessRules: Validates business rules on entity and returns warnings
/// 
/// This separation allows for:
/// - Clear distinction between errors (must fix) and warnings (should review)
/// - Flexible validation that can be applied at different stages
/// - Comprehensive feedback to users about data quality issues
/// 
/// ERROR VS WARNING:
/// - Errors: Data that cannot be processed (e.g., missing required field, invalid date)
/// - Warnings: Data that can be processed but may indicate issues (e.g., unusual quantity, status transition)
/// 
/// DUPLICATE DETECTION:
/// The service checks for duplicate waybills based on:
/// - waybill_id + supplier_id + delivery_date combination
/// This ensures that the same waybill cannot be imported twice for the same supplier and delivery date.
/// </summary>
public interface IWaybillValidationService
{
    /// <summary>
    /// Validates a waybill DTO and returns any validation errors.
    /// 
    /// This method performs comprehensive validation including:
    /// - Required field checks
    /// - Data type validation (dates, decimals)
    /// - Format validation
    /// - Basic business rule validation
    /// 
    /// VALIDATION RULES:
    /// - All required fields must be present and non-empty
    /// - Dates must be in valid format and parseable
    /// - Decimal fields (quantity, prices) must be valid numbers
    /// - Status must be a valid enum value
    /// - Basic business rules (quantity range, date ordering)
    /// 
    /// </summary>
    /// <param name="dto">The waybill DTO to validate.</param>
    /// <param name="rowNumber">The row number in the CSV file (for error reporting).</param>
    /// <param name="rowData">The original CSV row data (for error reporting).</param>
    /// <param name="existingWaybillIds">Set of existing waybill IDs to check for duplicates.</param>
    /// <returns>List of validation errors. Empty list if validation passes.</returns>
    List<ImportErrorDto> ValidateWaybill(
        ImportWaybillDto dto, 
        int rowNumber, 
        string rowData,
        HashSet<string> existingWaybillIds);

    /// <summary>
    /// Validates business rules on a waybill entity and returns warnings.
    /// 
    /// This method checks business rules that don't prevent import but may indicate issues:
    /// - Status transition validation
    /// - Unusual quantity values
    /// - Price calculation discrepancies
    /// 
    /// WARNINGS VS ERRORS:
    /// - Warnings don't prevent import but alert users to potential issues
    /// - Errors prevent import and must be fixed
    /// 
    /// BUSINESS RULES CHECKED:
    /// - Status transitions (e.g., can't go from CANCELLED back to PENDING)
    /// - Quantity reasonableness (within expected range)
    /// - Price calculation accuracy (total = quantity × unit_price)
    /// 
    /// </summary>
    /// <param name="dto">The waybill DTO to validate business rules for.</param>
    /// <returns>List of warning messages. Empty list if no warnings.</returns>
    List<string> ValidateBusinessRules(ImportWaybillDto dto);

    /// <summary>
    /// Validates that the tenant ID in the CSV matches the expected tenant ID from the header.
    /// This is a critical security check to prevent tenants from importing data for other tenants.
    /// </summary>
    /// <param name="csvTenantId">The tenant ID from the CSV file.</param>
    /// <param name="expectedTenantId">The tenant ID from the HTTP header (expected value).</param>
    /// <param name="rowNumber">The row number in the CSV (for error reporting).</param>
    /// <param name="rowData">The raw row data (for error reporting).</param>
    /// <returns>List of validation errors (empty if valid).</returns>
    List<ImportErrorDto> ValidateTenantIdMatch(
        string? csvTenantId,
        string expectedTenantId,
        int rowNumber,
        string rowData);
}
