using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Interfaces;

public interface IMasterDataRepository
{
    Task<List<MasterDataItem>> GetAllByCategoryAsync(MasterDataCategory category, bool? isActive = null, CancellationToken ct = default);
    Task<MasterDataItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(MasterDataCategory category, string code, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(MasterDataItem item, CancellationToken ct = default);
    Task UpdateAsync(MasterDataItem item, CancellationToken ct = default);
}
