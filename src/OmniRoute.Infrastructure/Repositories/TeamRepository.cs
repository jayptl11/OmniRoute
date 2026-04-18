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
}
