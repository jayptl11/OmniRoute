using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class MasterDataRepository : IMasterDataRepository
{
    private readonly AppDbContext _context;

    public MasterDataRepository(AppDbContext context) => _context = context;

    public async Task<List<MasterDataItem>> GetAllByCategoryAsync(
        MasterDataCategory category,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = _context.MasterDataItems.Where(x => x.Category == category);
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);
        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName).ToListAsync(ct);
    }

    public async Task<MasterDataItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.MasterDataItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ExistsByCodeAsync(
        MasterDataCategory category,
        string code,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _context.MasterDataItems.Where(x => x.Category == category && x.Code == code);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(MasterDataItem item, CancellationToken ct = default)
        => await _context.MasterDataItems.AddAsync(item, ct);

    public Task UpdateAsync(MasterDataItem item, CancellationToken ct = default)
    {
        _context.MasterDataItems.Update(item);
        return Task.CompletedTask;
    }
}
