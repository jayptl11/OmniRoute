using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class SlaConfigRepository : ISlaConfigRepository
{
    private readonly AppDbContext _context;

    public SlaConfigRepository(AppDbContext context) => _context = context;

    public async Task<SlaConfig?> GetByGroupAndPriorityAsync(AssignedGroup group, PriorityLevel priority, CancellationToken ct = default)
        => await _context.SlaConfigs
            .FirstOrDefaultAsync(x => x.IsActive && x.AssignedGroup == group && x.PriorityLevel == priority, ct);

    public async Task<List<SlaConfig>> GetAllAsync(CancellationToken ct = default)
        => await _context.SlaConfigs
            .OrderBy(x => x.AssignedGroup)
            .ThenBy(x => x.PriorityLevel)
            .ToListAsync(ct);

    public async Task<SlaConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SlaConfigs.FindAsync([id], ct);

    public Task UpdateAsync(SlaConfig slaConfig, CancellationToken ct = default)
    {
        _context.SlaConfigs.Update(slaConfig);
        return Task.CompletedTask;
    }
}
