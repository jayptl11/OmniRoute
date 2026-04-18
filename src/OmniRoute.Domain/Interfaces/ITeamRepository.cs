using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Interfaces;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Team>> GetActiveTeamsByGroupAsync(AssignedGroup teamType, CancellationToken ct = default);
}
