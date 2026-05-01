using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class AiApiKeyRepository : IAiApiKeyRepository
{
    private readonly AppDbContext _context;

    public AiApiKeyRepository(AppDbContext context) => _context = context;

    public async Task<List<AiApiKey>> GetAllAsync(CancellationToken ct = default)
        => await _context.AiApiKeys
            .OrderBy(k => k.Priority)
            .ThenBy(k => k.Provider)
            .ToListAsync(ct);

    public async Task<AiApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AiApiKeys.FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<List<AiApiKey>> GetActiveKeysOrderedByPriorityAsync(CancellationToken ct = default)
        => await _context.AiApiKeys
            .Where(k => k.IsActive)
            .OrderBy(k => k.Priority)
            .ToListAsync(ct);

    public async Task AddAsync(AiApiKey key, CancellationToken ct = default)
        => await _context.AiApiKeys.AddAsync(key, ct);

    public Task UpdateAsync(AiApiKey key, CancellationToken ct = default)
    {
        _context.AiApiKeys.Update(key);
        return Task.CompletedTask;
    }
}
