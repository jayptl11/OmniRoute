using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class ActivityLogRepository : IActivityLogRepository
{
    private readonly AppDbContext _context;

    public ActivityLogRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(ActivityLog log, CancellationToken ct = default)
        => await _context.ActivityLogs.AddAsync(log, ct);
}
