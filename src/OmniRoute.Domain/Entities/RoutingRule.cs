using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Entities;

public class RoutingRule
{
    public Guid Id { get; private set; }
    public string RuleName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int PriorityOrder { get; private set; }

    // Conditions — stored as JSON strings, exposed as arrays
    public string? ConditionChannelJson { get; private set; }
    public string? ConditionKeywordsJson { get; private set; }

    // Action
    public AssignedGroup ActionGroup { get; private set; }
    public Guid? ActionTeamId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public virtual Team? ActionTeam { get; set; }

    private RoutingRule() { } // EF Core

    public static RoutingRule Create(
        string ruleName,
        int priorityOrder,
        AssignedGroup actionGroup,
        string? description = null,
        string? conditionChannelJson = null,
        string? conditionKeywordsJson = null,
        Guid? actionTeamId = null)
    {
        var now = DateTime.UtcNow;
        return new RoutingRule
        {
            Id = Guid.NewGuid(),
            RuleName = ruleName,
            Description = description,
            PriorityOrder = priorityOrder,
            ConditionChannelJson = conditionChannelJson,
            ConditionKeywordsJson = conditionKeywordsJson,
            ActionGroup = actionGroup,
            ActionTeamId = actionTeamId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
