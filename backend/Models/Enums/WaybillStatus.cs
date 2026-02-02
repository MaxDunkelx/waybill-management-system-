namespace WaybillManagementSystem.Models.Enums;

/// <summary>
/// Represents the current status of a waybill in the system.
/// This enum tracks the lifecycle of a waybill from creation to final delivery or cancellation.
/// </summary>
public enum WaybillStatus
{
    /// <summary>
    /// Waybill has been created but delivery has not yet occurred.
    /// This is the initial state when a waybill is first entered into the system.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Waybill has been successfully delivered to the destination.
    /// This status indicates the goods have reached their intended location.
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Waybill has been cancelled and will not be delivered.
    /// This status is used when an order is cancelled before or during delivery.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Waybill delivery is disputed, typically due to quantity discrepancies,
    /// quality issues, or other delivery problems requiring resolution.
    /// This status triggers additional review and resolution workflows.
    /// </summary>
    Disputed = 3
}
