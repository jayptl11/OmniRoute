using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface INotificationConfigRepository
{
    Task<List<NotificationConfig>> GetAllAsync(CancellationToken ct = default);
    Task<List<string>> GetEnabledRolesForTypeAsync(string notificationType, CancellationToken ct = default);
    Task<NotificationConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
