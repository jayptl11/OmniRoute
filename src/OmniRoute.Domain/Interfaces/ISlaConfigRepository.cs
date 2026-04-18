using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Interfaces;

public interface ISlaConfigRepository
{
    Task<SlaConfig?> GetByGroupAndPriorityAsync(AssignedGroup group, PriorityLevel priority, CancellationToken ct = default);
}
