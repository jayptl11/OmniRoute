using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
}
