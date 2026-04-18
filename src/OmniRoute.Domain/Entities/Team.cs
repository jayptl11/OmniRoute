using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Entities;

public class Team
{
    public Guid Id { get; private set; }
    public string TeamName { get; private set; } = string.Empty;
    public AssignedGroup TeamType { get; private set; }
    public Guid? LeaderId { get; private set; }
    public Guid? StoreId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual User? Leader { get; set; }
    public virtual Store? Store { get; set; }

    private Team() { } // EF Core

    public static Team Create(string teamName, AssignedGroup teamType, Guid? leaderId = null, Guid? storeId = null)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            TeamName = teamName,
            TeamType = teamType,
            LeaderId = leaderId,
            StoreId = storeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
