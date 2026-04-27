using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.RoutingRules.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.RoutingRules.Queries.GetRoutingRules;

internal sealed class GetRoutingRulesQueryHandler : IQueryHandler<GetRoutingRulesQuery, List<RoutingRuleDto>>
{
    private readonly IRoutingRuleRepository _repository;

    public GetRoutingRulesQueryHandler(IRoutingRuleRepository repository)
        => _repository = repository;

    public async Task<Result<List<RoutingRuleDto>>> Handle(GetRoutingRulesQuery request, CancellationToken ct)
    {
        var rules = await _repository.GetAllOrderedAsync(ct);
        var dtos = rules.Select(MapToDto).ToList();
        return Result<List<RoutingRuleDto>>.Success(dtos);
    }

    private static RoutingRuleDto MapToDto(RoutingRule rule) => new(
        rule.Id,
        rule.RuleName,
        rule.Description,
        rule.PriorityOrder,
        Deserialize(rule.ConditionChannelJson),
        Deserialize(rule.ConditionKeywordsJson),
        rule.ActionGroup.ToString(),
        rule.ActionTeamId,
        rule.ActionTeam?.TeamName,
        rule.IsActive,
        rule.CreatedAt,
        rule.UpdatedAt);

    private static List<string>? Deserialize(string? json)
        => json is null ? null : JsonSerializer.Deserialize<List<string>>(json);
}
