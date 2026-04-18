using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context) => _context = context;

    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Stores.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<Store>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Stores.Where(x => x.IsActive).ToListAsync(ct);
}
