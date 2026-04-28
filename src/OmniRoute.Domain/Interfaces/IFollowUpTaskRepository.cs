namespace OmniRoute.Domain.Interfaces;

public interface IFollowUpTaskRepository
{
    Task<OmniRoute.Domain.Entities.FollowUpTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(OmniRoute.Domain.Entities.FollowUpTask task, CancellationToken ct = default);
    Task<List<OmniRoute.Domain.Entities.FollowUpTask>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
