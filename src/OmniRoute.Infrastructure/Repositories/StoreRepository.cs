using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context) => _context = context;

    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Stores
            .Include(s => s.Manager)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<Store>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Stores.Where(x => x.IsActive).ToListAsync(ct);

    public async Task<List<Store>> GetAllAsync(string? search = null, string? region = null, bool? isActive = null, CancellationToken ct = default)
    {
        var query = _context.Stores.Include(s => s.Manager).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.StoreName.Contains(search) || x.StoreCode.Contains(search));
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(x => x.Region != null && x.Region.Contains(region));
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);
        return await query.OrderBy(x => x.StoreName).ToListAsync(ct);
    }

    public async Task<bool> ExistsByCodeAsync(string storeCode, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Stores.Where(x => x.StoreCode == storeCode);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Store store, CancellationToken ct = default)
        => await _context.Stores.AddAsync(store, ct);

    public Task UpdateAsync(Store store, CancellationToken ct = default)
    {
        _context.Stores.Update(store);
        return Task.CompletedTask;
    }

    // DP-03: Danh sách tất cả cửa hàng kèm số lead đang active
    public async Task<List<(Store Store, int ActiveLeads)>> GetStoresWithActiveLeadCountAsync(
        CancellationToken ct = default)
    {
        var activeStatuses = new[]
        {
            LeadStatus.Assigned,
            LeadStatus.Contacted,
            LeadStatus.InProgress
        };

        var stores = await _context.Stores
            .OrderBy(s => s.StoreName)
            .ToListAsync(ct);

        var activeLeadCounts = await _context.Leads
            .Where(l => l.AssignedStoreId.HasValue && activeStatuses.Contains(l.Status))
            .GroupBy(l => l.AssignedStoreId!.Value)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countDict = activeLeadCounts.ToDictionary(x => x.StoreId, x => x.Count);

        return stores
            .Select(s => (s, countDict.TryGetValue(s.Id, out var c) ? c : 0))
            .ToList();
    }
}
