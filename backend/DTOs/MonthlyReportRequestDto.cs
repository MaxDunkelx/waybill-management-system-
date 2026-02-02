namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for monthly report generation request.
/// </summary>
public class MonthlyReportRequestDto
{
    /// <summary>
    /// Year for the monthly report (e.g., 2024).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month for the monthly report (1-12).
    /// </summary>
    public int Month { get; set; }
}
