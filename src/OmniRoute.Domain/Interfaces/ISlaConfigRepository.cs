using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Interfaces;

public interface ISlaConfigRepository
{
    Task<SlaConfig?> GetByGroupAndPriorityAsync(AssignedGroup group, PriorityLevel priority, CancellationToken ct = default);
    Task<List<SlaConfig>> GetAllAsync(CancellationToken ct = default);
    Task<SlaConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(SlaConfig slaConfig, CancellationToken ct = default);
}
