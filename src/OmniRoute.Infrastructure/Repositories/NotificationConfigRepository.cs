using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class NotificationConfigRepository : INotificationConfigRepository
{
    private readonly AppDbContext _context;

    public NotificationConfigRepository(AppDbContext context) => _context = context;

    public async Task<List<NotificationConfig>> GetAllAsync(CancellationToken ct = default)
        => await _context.NotificationConfigs
            .AsNoTracking()
            .OrderBy(c => c.NotificationType)
            .ThenBy(c => c.TargetRole)
            .ToListAsync(ct);

    public async Task<List<string>> GetEnabledRolesForTypeAsync(string notificationType, CancellationToken ct = default)
        => await _context.NotificationConfigs
            .AsNoTracking()
            .Where(c => c.NotificationType == notificationType && c.IsEnabled)
            .Select(c => c.TargetRole)
            .ToListAsync(ct);

    public async Task<NotificationConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.NotificationConfigs.FirstOrDefaultAsync(c => c.Id == id, ct);
}
