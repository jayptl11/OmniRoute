using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.RoutingRules.DTOs;

namespace OmniRoute.Application.Features.RoutingRules.Queries.TestRoutingRule;

public record TestRoutingRuleQuery(
    string? NeedDescription,
    string? Channel) : IQuery<TestRoutingRuleResultDto>;
