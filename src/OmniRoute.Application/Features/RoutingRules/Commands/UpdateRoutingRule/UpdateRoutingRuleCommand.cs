using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.RoutingRules.Commands.UpdateRoutingRule;

public record UpdateRoutingRuleCommand(
    Guid Id,
    string RuleName,
    string? Description,
    int PriorityOrder,
    List<string>? ConditionChannels,
    List<string>? ConditionKeywords,
    AssignedGroup ActionGroup,
    Guid? ActionTeamId) : ICommand;
