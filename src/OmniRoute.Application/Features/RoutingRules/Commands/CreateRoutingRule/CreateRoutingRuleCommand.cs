using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.RoutingRules.Commands.CreateRoutingRule;

public record CreateRoutingRuleCommand(
    string RuleName,
    string? Description,
    int PriorityOrder,
    List<string>? ConditionChannels,
    List<string>? ConditionKeywords,
    AssignedGroup ActionGroup,
    Guid? ActionTeamId) : ICommand<Guid>;
