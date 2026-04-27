using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class RoutingRuleRepository : IRoutingRuleRepository
{
    private readonly AppDbContext _context;

    public RoutingRuleRepository(AppDbContext context) => _context = context;

    public async Task<List<RoutingRule>> GetActiveRulesOrderedAsync(CancellationToken ct = default)
        => await _context.RoutingRules
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(ct);

    public async Task<List<RoutingRule>> GetAllOrderedAsync(CancellationToken ct = default)
        => await _context.RoutingRules
            .Include(x => x.ActionTeam)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(ct);

    public async Task<RoutingRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.RoutingRules
            .Include(x => x.ActionTeam)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(RoutingRule rule, CancellationToken ct = default)
        => await _context.RoutingRules.AddAsync(rule, ct);

    public Task UpdateAsync(RoutingRule rule, CancellationToken ct = default)
    {
        _context.RoutingRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task<bool> IsPriorityOrderTakenAsync(int priorityOrder, Guid? excludeId = null, CancellationToken ct = default)
        => await _context.RoutingRules
            .AnyAsync(x => x.PriorityOrder == priorityOrder && (excludeId == null || x.Id != excludeId.Value), ct);
}
