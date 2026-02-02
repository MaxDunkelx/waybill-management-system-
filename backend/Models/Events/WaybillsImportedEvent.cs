namespace WaybillManagementSystem.Models.Events;

/// <summary>
/// Event published when waybills are imported from CSV.
/// 
/// PURPOSE:
/// This event is published to RabbitMQ after a successful CSV import operation.
/// It allows other parts of the system (or external systems) to react to waybill
/// imports without tight coupling to the import service.
/// 
/// EVENT-DRIVEN ARCHITECTURE:
/// This event enables an event-driven architecture where:
/// - Import service publishes events (producer)
/// - Other services subscribe to events (consumers)
/// - Services are decoupled - they don't need direct references
/// - Events can trigger multiple actions (notifications, statistics, audits, etc.)
/// 
/// USE CASES:
/// - Audit logging: Log all imports for compliance
/// - Statistics updates: Update dashboard statistics
/// - Notifications: Send email/SMS notifications to users
/// - Data synchronization: Sync data to external systems
/// - Analytics: Trigger analytics processing
/// 
/// EVENT STRUCTURE:
/// The event contains summary information about the import operation:
/// - TenantId: Which tenant performed the import
/// - ImportedCount: Total number of waybills imported
/// - SuccessCount: Number of successful imports
/// - ErrorCount: Number of failed imports
/// - Timestamp: When the import completed
/// 
/// </summary>
public class WaybillsImportedEvent
{
    /// <summary>
    /// The tenant ID that performed the import.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of waybills that were imported (successful + failed).
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Number of waybills successfully imported.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of waybills that failed to import (validation errors, etc.).
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Timestamp when the import operation completed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
