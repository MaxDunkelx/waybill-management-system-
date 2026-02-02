namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for monthly report result.
/// Contains comprehensive statistics for a specific month.
/// </summary>
public class MonthlyReportResultDto
{
    /// <summary>
    /// Year of the report.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the report (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Timestamp when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of waybills in the month.
    /// </summary>
    public int TotalWaybills { get; set; }

    /// <summary>
    /// Total quantity of goods delivered in the month.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount (in currency) for the month.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Breakdown of waybills by status.
    /// </summary>
    public List<StatusBreakdownDto> StatusBreakdown { get; set; } = new List<StatusBreakdownDto>();

    /// <summary>
    /// Top suppliers by delivery volume for the month.
    /// </summary>
    public List<SupplierReportDto> TopSuppliers { get; set; } = new List<SupplierReportDto>();

    /// <summary>
    /// Top projects by delivery volume for the month.
    /// </summary>
    public List<ProjectReportDto> TopProjects { get; set; } = new List<ProjectReportDto>();

    /// <summary>
    /// Breakdown by product code/type.
    /// </summary>
    public List<ProductReportDto> ProductBreakdown { get; set; } = new List<ProductReportDto>();
}

/// <summary>
/// Status breakdown for monthly report.
/// </summary>
public class StatusBreakdownDto
{
    /// <summary>
    /// Status name (PENDING, DELIVERED, CANCELLED, DISPUTED).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Count of waybills with this status.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total quantity for this status.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount for this status.
    /// </summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Supplier report data for monthly report.
/// </summary>
public class SupplierReportDto
{
    /// <summary>
    /// Supplier ID.
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Number of deliveries from this supplier.
    /// </summary>
    public int DeliveryCount { get; set; }

    /// <summary>
    /// Total quantity from this supplier.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount from this supplier.
    /// </summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Project report data for monthly report.
/// </summary>
public class ProjectReportDto
{
    /// <summary>
    /// Project ID.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Project name.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Number of waybills for this project.
    /// </summary>
    public int WaybillCount { get; set; }

    /// <summary>
    /// Total quantity for this project.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount for this project.
    /// </summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Product report data for monthly report.
/// </summary>
public class ProductReportDto
{
    /// <summary>
    /// Product code (e.g., B25, B30, B35, B40).
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Count of waybills for this product.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total quantity for this product.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total amount for this product.
    /// </summary>
    public decimal TotalAmount { get; set; }
}
