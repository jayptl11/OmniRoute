using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _context;

    public TeamRepository(AppDbContext context) => _context = context;

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Teams.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<Team>> GetActiveTeamsByGroupAsync(AssignedGroup teamType, CancellationToken ct = default)
        => await _context.Teams
            .Where(x => x.IsActive && x.TeamType == teamType)
            .ToListAsync(ct);

    public async Task<List<Team>> GetAllAsync(
        AssignedGroup? teamType = null,
        Guid? storeId = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = _context.Teams.AsQueryable();
        if (teamType.HasValue)
            query = query.Where(x => x.TeamType == teamType.Value);
        if (storeId.HasValue)
            query = query.Where(x => x.StoreId == storeId.Value);
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);
        return await query.OrderBy(x => x.TeamName).ToListAsync(ct);
    }

    public async Task AddAsync(Team team, CancellationToken ct = default)
        => await _context.Teams.AddAsync(team, ct);

    public Task UpdateAsync(Team team, CancellationToken ct = default)
    {
        _context.Teams.Update(team);
        return Task.CompletedTask;
    }
}
