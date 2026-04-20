using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Stores.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Stores.Queries.GetStoreById;

internal sealed class GetStoreByIdQueryHandler : IQueryHandler<GetStoreByIdQuery, StoreDto>
{
    private readonly IStoreRepository _repository;

    public GetStoreByIdQueryHandler(IStoreRepository repository) => _repository = repository;

    public async Task<Result<StoreDto>> Handle(GetStoreByIdQuery query, CancellationToken ct)
    {
        var store = await _repository.GetByIdAsync(query.Id, ct);
        if (store is null)
            return Result<StoreDto>.Failure("NOT_FOUND", "Store not found.");

        return Result<StoreDto>.Success(new StoreDto(
            store.Id, store.StoreCode, store.StoreName, store.Address,
            store.Region, store.ManagerId, store.MaxCapacity, store.IsActive, store.CreatedAt));
    }
}
