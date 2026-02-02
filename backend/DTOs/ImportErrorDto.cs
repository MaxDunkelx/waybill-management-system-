namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object representing an error that occurred during CSV import.
/// 
/// PURPOSE:
/// This DTO provides detailed information about errors encountered while parsing
/// or processing a CSV row. It includes the row number, field name, error message,
/// and the actual row data for debugging purposes.
/// 
/// USAGE:
/// ImportErrorDto instances are collected in ImportResultDto.Errors to provide
/// comprehensive feedback about what went wrong during the import process.
/// 
/// DEBUGGING:
/// The RowData property contains the original CSV row data, which is helpful for:
/// - Identifying problematic rows in the source CSV
/// - Debugging encoding issues
/// - Understanding data format problems
/// - Providing context to users about what data caused the error
/// </summary>
public class ImportErrorDto
{
    /// <summary>
    /// The row number in the CSV file where the error occurred.
    /// Row numbers are 1-based (first data row is row 1, header row is row 0).
    /// This helps users identify which row in their CSV file has the problem.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// The name of the field/column where the error occurred.
    /// This is the CSV column name (e.g., "waybill_date", "quantity", "product_name").
    /// If the error is not field-specific, this may be null or empty.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// A human-readable error message describing what went wrong.
    /// This message should be clear and actionable, helping users understand
    /// how to fix the problem in their CSV file.
    /// 
    /// Examples:
    /// - "waybill_date is required but was empty"
    /// - "quantity must be a valid decimal number"
    /// - "Invalid date format. Expected YYYY-MM-DD"
    /// - "project_id 'PRJ999' does not exist"
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The actual CSV row data that caused the error.
    /// This is the raw row content from the CSV file, preserved for debugging.
    /// 
    /// WHY THIS IS IMPORTANT:
    /// - Helps identify encoding issues (if Hebrew text appears corrupted)
    /// - Allows users to see exactly what data caused the error
    /// - Useful for support teams to diagnose problems
    /// - Can be used to reconstruct the problematic row
    /// 
    /// NOTE: This may contain sensitive data, so be careful when logging or
    /// displaying this information in production environments.
    /// </summary>
    public string RowData { get; set; } = string.Empty;
}
