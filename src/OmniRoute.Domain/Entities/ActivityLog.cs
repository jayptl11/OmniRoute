namespace OmniRoute.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty; // LEAD|TICKET|USER|RULE|SYSTEM
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; } // JSON
    public string? NewValue { get; private set; } // JSON
    public string? Note { get; private set; }
    public Guid? PerformedBy { get; private set; } // null = system
    public DateTime PerformedAt { get; private set; }

    // Navigation
    public virtual User? PerformedByUser { get; set; }

    private ActivityLog() { } // EF Core

    public static ActivityLog Create(
        string entityType,
        Guid entityId,
        string action,
        Guid? performedBy = null,
        string? oldValue = null,
        string? newValue = null,
        string? note = null)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            Note = note,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow
        };
    }
}
