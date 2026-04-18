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
}
