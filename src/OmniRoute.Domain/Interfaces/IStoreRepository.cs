using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Store>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<Store>> GetAllAsync(string? region = null, bool? isActive = null, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string storeCode, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Store store, CancellationToken ct = default);
    Task UpdateAsync(Store store, CancellationToken ct = default);

    // DP-03: Tình trạng từng cửa hàng (kèm số lead đang active)
    Task<List<(Store Store, int ActiveLeads)>> GetStoresWithActiveLeadCountAsync(CancellationToken ct = default);
}
