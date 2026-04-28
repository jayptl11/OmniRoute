namespace OmniRoute.Domain.Entities;

public class FollowUpTask
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime DueAt { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual Lead? Lead { get; set; }
    public virtual User? User { get; set; }

    private FollowUpTask() { } // EF Core

    public static FollowUpTask Create(Guid leadId, Guid userId, DateTime dueAt, string note)
    {
        return new FollowUpTask
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            UserId = userId,
            DueAt = dueAt,
            Note = note,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }
}
