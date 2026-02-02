using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for updating waybill status.
/// 
/// PURPOSE:
/// This DTO represents a request to update a waybill's status. It includes the
/// new status and an optional notes field to document the reason for the status change.
/// 
/// USAGE:
/// This DTO is used in PATCH /api/waybills/{id}/status endpoint to update a waybill's
/// status. The status transition must comply with business rules (see status transition
/// rules in WaybillService.UpdateStatusAsync).
/// 
/// STATUS TRANSITION RULES:
/// The following status transitions are allowed:
/// - PENDING → DELIVERED (waybill was successfully delivered)
/// - PENDING → CANCELLED (waybill was cancelled before delivery)
/// - DELIVERED → DISPUTED (delivery has issues or disputes)
/// 
/// The following transitions are NOT allowed:
/// - CANCELLED → anything (cancelled waybills cannot be changed)
/// - Any other transition not listed above
/// 
/// NOTES FIELD:
/// The Notes field is optional but recommended for documenting the reason for status
/// changes, especially for CANCELLED or DISPUTED statuses. This helps with audit trails
/// and understanding why a waybill's status was changed.
/// 
/// EXAMPLE:
/// {
///   "status": "Delivered",
///   "notes": "Successfully delivered to site at 14:30"
/// }
/// </summary>
[SwaggerSchema(Description = "Request to update a waybill's status with optional notes")]
public class UpdateWaybillStatusDto
{
    /// <summary>
    /// The new status for the waybill.
    /// 
    /// Valid values: Pending, Delivered, Cancelled, Disputed
    /// 
    /// The status transition must comply with business rules:
    /// - PENDING → DELIVERED or CANCELLED (allowed)
    /// - DELIVERED → DISPUTED (allowed)
    /// - CANCELLED → anything (NOT allowed)
    /// - Any other transition (NOT allowed)
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [SwaggerSchema(Description = "New status for the waybill. Must comply with status transition rules. Example: Delivered")]
    public WaybillStatus Status { get; set; }

    /// <summary>
    /// Optional notes documenting the reason for the status change.
    /// 
    /// This field is recommended for:
    /// - CANCELLED status: Explain why the waybill was cancelled
    /// - DISPUTED status: Describe the dispute or issue
    /// - DELIVERED status: Any relevant delivery notes
    /// 
    /// Maximum length: 2000 characters (matches Waybill.Notes field)
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    [SwaggerSchema(Description = "Optional notes explaining the status change. Recommended for audit trail. Example: Successfully delivered to site at 14:30")]
    public string? Notes { get; set; }
}
