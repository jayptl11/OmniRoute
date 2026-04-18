using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Store>> GetAllActiveAsync(CancellationToken ct = default);
}
