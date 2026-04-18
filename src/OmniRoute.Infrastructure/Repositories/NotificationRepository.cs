using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await _context.Notifications.AddAsync(notification, ct);
}
