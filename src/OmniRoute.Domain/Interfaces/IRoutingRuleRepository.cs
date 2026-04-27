using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface IRoutingRuleRepository
{
    Task<List<RoutingRule>> GetActiveRulesOrderedAsync(CancellationToken ct = default);
    Task<List<RoutingRule>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<RoutingRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RoutingRule rule, CancellationToken ct = default);
    Task UpdateAsync(RoutingRule rule, CancellationToken ct = default);
    Task<bool> IsPriorityOrderTakenAsync(int priorityOrder, Guid? excludeId = null, CancellationToken ct = default);
}
