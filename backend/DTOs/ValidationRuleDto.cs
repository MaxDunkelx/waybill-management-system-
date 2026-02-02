namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object documenting validation rules for waybill imports.
/// 
/// PURPOSE:
/// This DTO serves as documentation for all validation rules applied during
/// CSV import. It provides a reference for developers and can be used to
/// generate validation documentation or API responses.
/// 
/// USAGE:
/// This is primarily for documentation purposes. The actual validation logic
/// is implemented in WaybillValidationService. This DTO can be used to:
/// - Generate API documentation
/// - Display validation rules to users
/// - Create validation rule reports
/// - Test validation coverage
/// 
/// NOTE:
/// This DTO is optional and may not be used in the actual import process.
/// It serves as a centralized reference for all validation rules.
/// </summary>
public class ValidationRuleDto
{
    /// <summary>
    /// Gets or sets the category of the validation rule.
    /// Categories: RequiredFields, DataTypes, BusinessRules, Duplicates
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the validation rule.
    /// Example: "QuantityRange", "PriceCalculation", "DateOrdering"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field(s) this rule applies to.
    /// Example: "quantity", "total_amount", "delivery_date"
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the validation rule.
    /// Explains what the rule checks and why it exists.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message shown when validation fails.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this rule generates an error (blocks import) or warning (allows import).
    /// </summary>
    public bool IsError { get; set; } = true;

    /// <summary>
    /// Gets or sets the severity level: Error, Warning, Info
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Gets all validation rules as a list for documentation purposes.
    /// </summary>
    /// <returns>List of all validation rules.</returns>
    public static List<ValidationRuleDto> GetAllRules()
    {
        return new List<ValidationRuleDto>
        {
            // Required Fields
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "WaybillIdRequired",
                Field = "waybill_id",
                Description = "waybill_id is required and cannot be empty",
                ErrorMessage = "waybill_id is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "WaybillDateRequired",
                Field = "waybill_date",
                Description = "waybill_date is required and cannot be empty",
                ErrorMessage = "waybill_date is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "DeliveryDateRequired",
                Field = "delivery_date",
                Description = "delivery_date is required and cannot be empty",
                ErrorMessage = "delivery_date is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "ProjectIdRequired",
                Field = "project_id",
                Description = "project_id is required and cannot be empty",
                ErrorMessage = "project_id is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "SupplierIdRequired",
                Field = "supplier_id",
                Description = "supplier_id is required and cannot be empty",
                ErrorMessage = "supplier_id is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "ProductCodeRequired",
                Field = "product_code",
                Description = "product_code is required and cannot be empty",
                ErrorMessage = "product_code is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "ProductNameRequired",
                Field = "product_name",
                Description = "product_name is required and cannot be empty",
                ErrorMessage = "product_name is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "QuantityRequired",
                Field = "quantity",
                Description = "quantity is required and cannot be empty",
                ErrorMessage = "quantity is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "UnitRequired",
                Field = "unit",
                Description = "unit is required and cannot be empty",
                ErrorMessage = "unit is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "UnitPriceRequired",
                Field = "unit_price",
                Description = "unit_price is required and cannot be empty",
                ErrorMessage = "unit_price is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "TotalAmountRequired",
                Field = "total_amount",
                Description = "total_amount is required and cannot be empty",
                ErrorMessage = "total_amount is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "RequiredFields",
                Name = "DeliveryAddressRequired",
                Field = "delivery_address",
                Description = "delivery_address is required and cannot be empty",
                ErrorMessage = "delivery_address is required and cannot be empty",
                IsError = true,
                Severity = "Error"
            },

            // Data Type Validation
            new ValidationRuleDto
            {
                Category = "DataTypes",
                Name = "DateFormat",
                Field = "waybill_date, delivery_date",
                Description = "Dates must be in valid format (YYYY-MM-DD, DD/MM/YYYY, MM/DD/YYYY)",
                ErrorMessage = "Date is not in a valid format. Expected: YYYY-MM-DD or DD/MM/YYYY",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "DataTypes",
                Name = "DecimalFormat",
                Field = "quantity, unit_price, total_amount",
                Description = "Decimal fields must be valid decimal numbers",
                ErrorMessage = "Value is not a valid decimal number",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "DataTypes",
                Name = "StatusEnum",
                Field = "status",
                Description = "Status must be a valid enum value: Pending, Delivered, Cancelled, Disputed",
                ErrorMessage = "Status is not valid. Valid values: Pending, Delivered, Cancelled, Disputed",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "DataTypes",
                Name = "CurrencyFormat",
                Field = "currency",
                Description = "Currency must be exactly 3 characters (e.g., ILS, USD)",
                ErrorMessage = "Currency must be exactly 3 characters",
                IsError = true,
                Severity = "Error"
            },

            // Business Rules
            new ValidationRuleDto
            {
                Category = "BusinessRules",
                Name = "QuantityRange",
                Field = "quantity",
                Description = "Quantity must be between 0.5 and 50",
                ErrorMessage = "Quantity is outside the allowed range (0.5 to 50)",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "BusinessRules",
                Name = "PriceCalculation",
                Field = "total_amount",
                Description = "total_amount must equal quantity × unit_price (within 0.01 tolerance for rounding)",
                ErrorMessage = "total_amount does not match calculated value (quantity × unit_price)",
                IsError = true,
                Severity = "Error"
            },
            new ValidationRuleDto
            {
                Category = "BusinessRules",
                Name = "DateOrdering",
                Field = "delivery_date",
                Description = "delivery_date cannot be before waybill_date",
                ErrorMessage = "delivery_date cannot be before waybill_date",
                IsError = true,
                Severity = "Error"
            },

            // Duplicate Detection
            new ValidationRuleDto
            {
                Category = "Duplicates",
                Name = "DuplicateWaybill",
                Field = "waybill_id, supplier_id, delivery_date",
                Description = "A waybill with the same waybill_id, supplier_id, and delivery_date cannot be imported twice",
                ErrorMessage = "Duplicate waybill detected. A waybill with the same waybill_id, supplier_id, and delivery_date already exists",
                IsError = true,
                Severity = "Error"
            },

            // Business Rule Warnings
            new ValidationRuleDto
            {
                Category = "BusinessRules",
                Name = "StatusTransition",
                Field = "status",
                Description = "Status transitions are validated. CANCELLED status cannot be changed later",
                ErrorMessage = "Status is CANCELLED. Once cancelled, a waybill cannot be changed to any other status",
                IsError = false,
                Severity = "Warning"
            },
            new ValidationRuleDto
            {
                Category = "BusinessRules",
                Name = "QuantityBoundary",
                Field = "quantity",
                Description = "Quantities near the boundaries (0.5 or 50) are flagged for review",
                ErrorMessage = "Quantity is very close to the minimum/maximum allowed value",
                IsError = false,
                Severity = "Warning"
            },
            new ValidationRuleDto
            {
                Category = "BusinessRules",
                Name = "PriceCalculationTolerance",
                Field = "total_amount",
                Description = "Price calculations close to the tolerance limit are flagged for review",
                ErrorMessage = "Price calculation difference is close to the tolerance limit",
                IsError = false,
                Severity = "Warning"
            }
        };
    }
}
