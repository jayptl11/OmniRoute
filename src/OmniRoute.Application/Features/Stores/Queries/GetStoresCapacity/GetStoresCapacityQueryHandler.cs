using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Stores.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Stores.Queries.GetStoresCapacity;

internal sealed class GetStoresCapacityQueryHandler
    : IQueryHandler<GetStoresCapacityQuery, List<StoreCapacityDto>>
{
    private readonly IStoreRepository _storeRepository;

    public GetStoresCapacityQueryHandler(IStoreRepository storeRepository)
        => _storeRepository = storeRepository;

    public async Task<Result<List<StoreCapacityDto>>> Handle(
        GetStoresCapacityQuery query,
        CancellationToken ct)
    {
        var storesWithCount = await _storeRepository.GetStoresWithActiveLeadCountAsync(query.Q, ct);

        var result = storesWithCount
            .Select(x =>
            {
                var available = x.Store.MaxCapacity - x.ActiveLeads;
                var isOver = available < 0;
                var isNear = !isOver && available < (int)Math.Ceiling(x.Store.MaxCapacity * 0.2);

                return new StoreCapacityDto(
                    Id: x.Store.Id,
                    StoreCode: x.Store.StoreCode,
                    StoreName: x.Store.StoreName,
                    Address: x.Store.Address,
                    Region: x.Store.Region,
                    ManagerId: x.Store.ManagerId,
                    MaxCapacity: x.Store.MaxCapacity,
                    ActiveLeads: x.ActiveLeads,
                    AvailableSlots: Math.Max(0, available),
                    IsOverCapacity: isOver,
                    IsNearCapacity: isNear,
                    IsActive: x.Store.IsActive);
            })
            .OrderBy(s => s.StoreName)
            .ToList();

        return Result<List<StoreCapacityDto>>.Success(result);
    }
}
