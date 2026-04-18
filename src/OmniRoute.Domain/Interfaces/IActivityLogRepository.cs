using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog log, CancellationToken ct = default);
}
