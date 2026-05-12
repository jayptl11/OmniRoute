using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.RoutingRules.DTOs;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.RoutingRules.Queries.TestRoutingRule;

internal sealed class TestRoutingRuleQueryHandler : IQueryHandler<TestRoutingRuleQuery, TestRoutingRuleResultDto>
{
    private readonly IRoutingRuleRepository _repository;

    public TestRoutingRuleQueryHandler(IRoutingRuleRepository repository)
        => _repository = repository;

    public async Task<Result<TestRoutingRuleResultDto>> Handle(TestRoutingRuleQuery request, CancellationToken ct)
    {
        var activeRules = await _repository.GetActiveRulesOrderedAsync(ct);

        foreach (var rule in activeRules)
        {
            if (!ChannelMatches(rule.ConditionChannelJson, request.Channel))
                continue;

            if (!KeywordsMatch(rule.ConditionKeywordsJson, request.NeedDescription))
                continue;

            var dto = new TestRoutingRuleResultDto(
                Matched: true,
                MatchedRuleId: rule.Id,
                MatchedRuleName: rule.RuleName,
                MatchedPriorityOrder: rule.PriorityOrder,
                ResultGroup: rule.ActionGroup.ToString());

            return Result<TestRoutingRuleResultDto>.Success(dto);
        }

        var noMatch = new TestRoutingRuleResultDto(
            Matched: false,
            MatchedRuleId: null,
            MatchedRuleName: null,
            MatchedPriorityOrder: null,
            ResultGroup: "StoreSupport");

        return Result<TestRoutingRuleResultDto>.Success(noMatch);
    }

    private static bool ChannelMatches(string? conditionChannelJson, string? channel)
        => RoutingRuleChannelHelper.RuleMatchesRequestedChannel(conditionChannelJson, channel);

    private static bool KeywordsMatch(string? conditionKeywordsJson, string? needDescription)
    {
        if (conditionKeywordsJson is null) return true;

        var keywords = JsonSerializer.Deserialize<List<string>>(conditionKeywordsJson);
        if (keywords is null || keywords.Count == 0) return true;

        if (needDescription is null) return false;

        return keywords.Any(k => needDescription.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
