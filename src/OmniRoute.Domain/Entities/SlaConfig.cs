using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Entities;

public class SlaConfig
{
    public Guid Id { get; private set; }
    public AssignedGroup AssignedGroup { get; private set; }
    public PriorityLevel PriorityLevel { get; private set; }
    public int MaxHours { get; private set; }
    public int WarningBeforeHours { get; private set; }
    public bool IsActive { get; private set; }

    private SlaConfig() { } // EF Core

    public static SlaConfig Create(AssignedGroup assignedGroup, PriorityLevel priorityLevel, int maxHours, int warningBeforeHours)
    {
        return new SlaConfig
        {
            Id = Guid.NewGuid(),
            AssignedGroup = assignedGroup,
            PriorityLevel = priorityLevel,
            MaxHours = maxHours,
            WarningBeforeHours = warningBeforeHours,
            IsActive = true
        };
    }

    public void Update(int maxHours, int warningBeforeHours)
    {
        MaxHours = maxHours;
        WarningBeforeHours = warningBeforeHours;
    }
}
