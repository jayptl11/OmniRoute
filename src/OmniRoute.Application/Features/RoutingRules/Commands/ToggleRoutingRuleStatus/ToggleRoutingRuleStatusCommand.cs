using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.RoutingRules.Commands.ToggleRoutingRuleStatus;

public record ToggleRoutingRuleStatusCommand(Guid Id, bool IsActive) : ICommand;
