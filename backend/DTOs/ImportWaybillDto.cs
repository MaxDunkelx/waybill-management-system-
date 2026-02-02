using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for importing waybill data from CSV files.
/// 
/// PURPOSE:
/// This DTO represents a single row from a CSV file containing waybill data.
/// It maps CSV column names to properties using CsvHelper attributes, allowing
/// flexible CSV formats while maintaining type safety.
/// 
/// CSV FORMAT EXPECTATIONS:
/// The CSV file should contain the following columns (in any order):
/// - waybill_id: Unique identifier for the waybill
/// - waybill_date: Date when the waybill was issued (format: YYYY-MM-DD or DD/MM/YYYY)
/// - delivery_date: Date when goods were/will be delivered (format: YYYY-MM-DD or DD/MM/YYYY)
/// - project_id: Identifier of the project receiving the delivery
/// - supplier_id: Identifier of the supplier providing the goods
/// - product_code: Product code (e.g., "B30", "C25")
/// - product_name: Product name in Hebrew
/// - quantity: Quantity delivered (decimal number)
/// - unit: Unit of measurement in Hebrew (e.g., "מ\"ק", "ק\"ג")
/// - unit_price: Price per unit (decimal number)
/// - total_amount: Total amount (decimal number)
/// - currency: Currency code (e.g., "ILS")
/// - status: Waybill status (Pending, Delivered, Cancelled, Disputed)
/// - vehicle_number: Vehicle license plate (optional)
/// - driver_name: Driver name in Hebrew (optional)
/// - delivery_address: Delivery address in Hebrew
/// - notes: Additional notes in Hebrew (optional)
/// 
/// ENCODING REQUIREMENTS:
/// The CSV file must be encoded in UTF-8 with BOM or UTF-8 without BOM to properly
/// handle Hebrew characters. The CsvHelper configuration in WaybillImportService
/// is set to use UTF-8 encoding explicitly.
/// 
/// HEBREW TEXT SUPPORT:
/// All text properties (product_name, unit, driver_name, delivery_address, notes)
/// are defined as string to support Hebrew Unicode characters. CsvHelper will
/// automatically handle UTF-8 encoded Hebrew text when the file is properly encoded.
/// </summary>
public class ImportWaybillDto
{
    /// <summary>
    /// Unique waybill identifier from the CSV file.
    /// Maps to the "waybill_id" column in the CSV.
    /// This will be used as the Waybill.Id in the database.
    /// </summary>
    [Name("waybill_id")]
    [Required]
    public string WaybillId { get; set; } = string.Empty;

    /// <summary>
    /// Date when the waybill was issued.
    /// Maps to the "waybill_date" column in the CSV.
    /// Expected format: YYYY-MM-DD or DD/MM/YYYY (configurable in CsvHelper).
    /// </summary>
    [Name("waybill_date")]
    [Required]
    public string WaybillDate { get; set; } = string.Empty;

    /// <summary>
    /// Date when the goods were or will be delivered.
    /// Maps to the "delivery_date" column in the CSV.
    /// Expected format: YYYY-MM-DD or DD/MM/YYYY (configurable in CsvHelper).
    /// </summary>
    [Name("delivery_date")]
    [Required]
    public string DeliveryDate { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the project receiving the delivery.
    /// Maps to the "project_id" column in the CSV.
    /// This must match an existing Project.Id in the database.
    /// </summary>
    [Name("project_id")]
    [Required]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the supplier providing the goods.
    /// Maps to the "supplier_id" column in the CSV.
    /// This must match an existing Supplier.Id in the database.
    /// </summary>
    [Name("supplier_id")]
    [Required]
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Product code identifier (e.g., "B30", "C25").
    /// Maps to the "product_code" column in the CSV.
    /// </summary>
    [Name("product_code")]
    [Required]
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Product name in Hebrew.
    /// Maps to the "product_name" column in the CSV.
    /// This field supports Hebrew Unicode characters and must be in UTF-8 encoding.
    /// </summary>
    [Name("product_name")]
    [Required]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of goods delivered.
    /// Maps to the "quantity" column in the CSV.
    /// Expected format: decimal number (e.g., "10.5", "100").
    /// </summary>
    [Name("quantity")]
    [Required]
    public string Quantity { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measurement in Hebrew (e.g., "מ\"ק", "ק\"ג", "יח'").
    /// Maps to the "unit" column in the CSV.
    /// This field supports Hebrew Unicode characters and must be in UTF-8 encoding.
    /// </summary>
    [Name("unit")]
    [Required]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit.
    /// Maps to the "unit_price" column in the CSV.
    /// Expected format: decimal number (e.g., "150.50", "1000").
    /// </summary>
    [Name("unit_price")]
    [Required]
    public string UnitPrice { get; set; } = string.Empty;

    /// <summary>
    /// Total amount for the waybill.
    /// Maps to the "total_amount" column in the CSV.
    /// Expected format: decimal number (e.g., "1505.00", "10000").
    /// </summary>
    [Name("total_amount")]
    [Required]
    public string TotalAmount { get; set; } = string.Empty;

    /// <summary>
    /// Currency code (e.g., "ILS", "USD", "EUR").
    /// Maps to the "currency" column in the CSV.
    /// Defaults to "ILS" if not provided.
    /// </summary>
    [Name("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Waybill status.
    /// Maps to the "status" column in the CSV.
    /// Expected values: "Pending", "Delivered", "Cancelled", "Disputed"
    /// Case-insensitive matching will be performed.
    /// </summary>
    [Name("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Vehicle license plate number.
    /// Maps to the "vehicle_number" column in the CSV.
    /// This field is optional - null if not provided in the CSV.
    /// </summary>
    [Name("vehicle_number")]
    public string? VehicleNumber { get; set; }

    /// <summary>
    /// Driver name in Hebrew.
    /// Maps to the "driver_name" column in the CSV.
    /// This field is optional and supports Hebrew Unicode characters.
    /// Must be in UTF-8 encoding if provided.
    /// </summary>
    [Name("driver_name")]
    public string? DriverName { get; set; }

    /// <summary>
    /// Delivery address in Hebrew.
    /// Maps to the "delivery_address" column in the CSV.
    /// This field supports Hebrew Unicode characters and must be in UTF-8 encoding.
    /// </summary>
    [Name("delivery_address")]
    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes in Hebrew.
    /// Maps to the "notes" column in the CSV.
    /// This field is optional and supports Hebrew Unicode characters.
    /// Must be in UTF-8 encoding if provided.
    /// </summary>
    [Name("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Project name in Hebrew.
    /// Maps to the "project_name" column in the CSV.
    /// This field is used when creating a new project if it doesn't exist.
    /// Supports Hebrew Unicode characters and must be in UTF-8 encoding.
    /// </summary>
    [Name("project_name")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Supplier name in Hebrew.
    /// Maps to the "supplier_name" column in the CSV.
    /// This field is used when creating a new supplier if it doesn't exist.
    /// Supports Hebrew Unicode characters and must be in UTF-8 encoding.
    /// </summary>
    [Name("supplier_name")]
    public string? SupplierName { get; set; }

    /// <summary>
    /// Creation timestamp from the CSV file.
    /// Maps to the "created_at" column in the CSV.
    /// Expected format: ISO 8601 (e.g., "2024-09-01T08:30:00Z").
    /// If provided, this will be used as the CreatedAt timestamp for the waybill.
    /// If not provided or invalid, the current UTC time will be used.
    /// </summary>
    [Name("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// Tenant ID from the CSV file.
    /// Maps to the "tenant_id" column in the CSV.
    /// 
    /// SECURITY NOTE:
    /// This value is used for VALIDATION ONLY. The actual tenant ID used for
    /// database operations comes from the X-Tenant-ID HTTP header (set by TenantMiddleware).
    /// The CSV tenant_id must match the header tenant_id to prevent data leakage
    /// (e.g., a tenant trying to import data for another tenant).
    /// 
    /// If the CSV tenant_id does not match the header tenant_id, the import
    /// will fail with a validation error.
    /// </summary>
    [Name("tenant_id")]
    public string? TenantId { get; set; }
}
