using WaybillManagementSystem.DTOs;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for importing waybill data from CSV files.
/// 
/// PURPOSE:
/// This service handles the parsing of CSV files containing waybill data,
/// converting CSV rows into structured DTOs that can be validated and
/// inserted into the database.
/// 
/// RESPONSIBILITIES:
/// - Parse CSV files with proper encoding (UTF-8 for Hebrew support)
/// - Map CSV columns to ImportWaybillDto properties
/// - Handle date parsing with multiple format support
/// - Collect parsing errors and warnings
/// - Return comprehensive import results
/// 
/// CSV FORMAT:
/// The service expects CSV files with specific columns (see ImportWaybillDto
/// for column names). The CSV must be UTF-8 encoded to support Hebrew text.
/// 
/// ERROR HANDLING:
/// The service follows a "best effort" strategy - it continues processing
/// even if some rows have errors, collecting all errors and returning them
/// together. This allows users to see all problems at once.
/// 
/// FUTURE ENHANCEMENTS:
/// In future parts, this service will also:
/// - Validate parsed data against business rules
/// - Check referential integrity (projects, suppliers exist)
/// - Insert validated data into the database
/// - Handle duplicate waybill IDs
/// - Support batch processing for large files
/// </summary>
public interface IWaybillImportService
{
    /// <summary>
    /// Imports waybill data from a CSV stream.
    /// 
    /// This method parses the CSV file and converts each row into an
    /// ImportWaybillDto. It handles encoding, date parsing, and error
    /// collection, returning a comprehensive ImportResultDto with all
    /// parsed data and any errors encountered.
    /// 
    /// PROCESSING FLOW:
    /// 1. Configure CsvHelper for UTF-8 encoding and Hebrew support
    /// 2. Read CSV stream and detect header row
    /// 3. Parse each row into ImportWaybillDto
    /// 4. Collect parsing errors for failed rows
    /// 5. Return ImportResultDto with all parsed data and errors
    /// 
    /// ENCODING:
    /// The CSV stream must be UTF-8 encoded to properly handle Hebrew characters.
    /// The service explicitly configures CsvHelper to use UTF-8 encoding.
    /// 
    /// TENANT ISOLATION:
    /// The tenantId parameter ensures that imported waybills are associated
    /// with the correct tenant. This is important for multi-tenant data isolation.
    /// 
    /// ERROR HANDLING:
    /// If a row fails to parse, an error is recorded but processing continues.
    /// All errors are collected and returned in the ImportResultDto.Errors list.
    /// 
    /// </summary>
    /// <param name="csvStream">The CSV file stream to parse. Must be UTF-8 encoded.</param>
    /// <param name="tenantId">The tenant ID to associate with imported waybills.</param>
    /// <returns>
    /// An ImportResultDto containing:
    /// - Total rows processed
    /// - Successfully parsed waybills
    /// - Errors for failed rows
    /// - Warnings for non-critical issues
    /// </returns>
    Task<ImportResultDto> ImportFromCsvAsync(Stream csvStream, string tenantId);
}
