namespace OmniRoute.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty; // SLA_WARNING|SLA_VIOLATED|NEW_LEAD|ESCALATED|SYSTEM_ALERT
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty; // LEAD|TICKET|SYSTEM
    public Guid EntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual User? User { get; set; }

    private Notification() { } // EF Core

    public static Notification Create(
        Guid userId,
        string type,
        string title,
        string body,
        string entityType,
        Guid entityId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead() => IsRead = true;
}
