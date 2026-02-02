namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object representing the result of a CSV import operation.
/// 
/// PURPOSE:
/// This DTO provides comprehensive feedback about the CSV import process, including:
/// - Total number of rows processed
/// - Number of successfully parsed rows
/// - Number of rows with errors
/// - Detailed error information for each failed row
/// - Warnings for non-critical issues
/// - The parsed waybill data (for successful imports)
/// 
/// USAGE:
/// The import service returns this DTO to provide complete visibility into the
/// import process. Controllers can use this information to:
/// - Display import statistics to users
/// - Show detailed error messages for failed rows
/// - Provide warnings about potential issues
/// - Process successfully parsed data
/// 
/// ERROR HANDLING STRATEGY:
/// The import process follows a "best effort" strategy:
/// - Continue processing even if some rows have errors
/// - Collect all errors and return them together
/// - Allow partial imports (some rows succeed, some fail)
/// - Provide detailed error information for each failed row
/// 
/// This approach allows users to fix their CSV files incrementally and see
/// all problems at once rather than fixing one error at a time.
/// </summary>
public class ImportResultDto
{
    /// <summary>
    /// Total number of rows processed from the CSV file.
    /// This includes both successful and failed rows.
    /// The header row is not counted in this total.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Number of rows that were successfully parsed without errors.
    /// These rows are ready for validation and database insertion (in future steps).
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of rows that had errors during parsing.
    /// These rows could not be parsed correctly and need to be fixed in the CSV file.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of detailed error information for each row that failed to parse.
    /// Each ImportErrorDto contains:
    /// - Row number in the CSV file
    /// - Field name where the error occurred
    /// - Error message
    /// - Original row data for debugging
    /// 
    /// This list is empty if all rows were successfully parsed.
    /// </summary>
    public List<ImportErrorDto> Errors { get; set; } = new List<ImportErrorDto>();

    /// <summary>
    /// List of warning messages for non-critical issues encountered during import.
    /// Warnings indicate potential problems that don't prevent parsing but may
    /// cause issues during validation or database insertion.
    /// 
    /// Examples:
    /// - "Some rows have missing optional fields"
    /// - "Date format varies across rows"
    /// - "Currency code not specified, defaulting to ILS"
    /// 
    /// This list is empty if no warnings were generated.
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();

    /// <summary>
    /// List of successfully parsed waybill data.
    /// This contains ImportWaybillDto objects for each row that was parsed
    /// without errors. These DTOs are ready for validation and database insertion
    /// (in future steps).
    /// 
    /// NOTE: In PART 1 (parsing only), this list contains all parsed rows.
    /// In future parts, this will only contain validated rows.
    /// </summary>
    public List<ImportWaybillDto> ParsedWaybills { get; set; } = new List<ImportWaybillDto>();
}
