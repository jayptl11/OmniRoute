using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Stores.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Stores.Queries.GetStores;

internal sealed class GetStoresQueryHandler : IQueryHandler<GetStoresQuery, List<StoreDto>>
{
    private readonly IStoreRepository _repository;

    public GetStoresQueryHandler(IStoreRepository repository) => _repository = repository;

    public async Task<Result<List<StoreDto>>> Handle(GetStoresQuery query, CancellationToken ct)
    {
        var stores = await _repository.GetAllAsync(query.Search, query.Region, query.IsActive, ct);
        var dtos = stores.Select(s => new StoreDto(
            s.Id, s.StoreCode, s.StoreName, s.Address, s.Region,
            s.ManagerId,
            s.Manager is null ? null : $"{s.Manager.FirstName} {s.Manager.LastName}".Trim(),
            s.Manager?.Username,
            s.MaxCapacity, s.IsActive, s.CreatedAt))
            .ToList();
        return Result<List<StoreDto>>.Success(dtos);
    }
}
