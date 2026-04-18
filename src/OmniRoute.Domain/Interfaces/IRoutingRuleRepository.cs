using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface IRoutingRuleRepository
{
    Task<List<RoutingRule>> GetActiveRulesOrderedAsync(CancellationToken ct = default);
}
