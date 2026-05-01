using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface IAiApiKeyRepository
{
    Task<List<AiApiKey>> GetAllAsync(CancellationToken ct = default);
    Task<AiApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AiApiKey>> GetActiveKeysOrderedByPriorityAsync(CancellationToken ct = default);
    Task AddAsync(AiApiKey key, CancellationToken ct = default);
    Task UpdateAsync(AiApiKey key, CancellationToken ct = default);
}
