using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.RoutingRules.DTOs;

namespace OmniRoute.Application.Features.RoutingRules.Queries.GetRoutingRules;

public record GetRoutingRulesQuery : IQuery<List<RoutingRuleDto>>;
