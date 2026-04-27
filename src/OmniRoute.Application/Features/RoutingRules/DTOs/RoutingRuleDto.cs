namespace OmniRoute.Application.Features.RoutingRules.DTOs;

public record RoutingRuleDto(
    Guid Id,
    string RuleName,
    string? Description,
    int PriorityOrder,
    List<string>? ConditionChannels,
    List<string>? ConditionKeywords,
    string ActionGroup,
    Guid? ActionTeamId,
    string? ActionTeamName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TestRoutingRuleResultDto(
    bool Matched,
    Guid? MatchedRuleId,
    string? MatchedRuleName,
    int? MatchedPriorityOrder,
    string ResultGroup);
