using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.MasterData.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.MasterData.Queries.GetMasterDataItems;

internal sealed class GetMasterDataItemsQueryHandler
    : IQueryHandler<GetMasterDataItemsQuery, List<MasterDataItemDto>>
{
    private readonly IMasterDataRepository _repository;

    public GetMasterDataItemsQueryHandler(IMasterDataRepository repository) => _repository = repository;

    public async Task<Result<List<MasterDataItemDto>>> Handle(
        GetMasterDataItemsQuery query,
        CancellationToken ct)
    {
        var items = await _repository.GetAllByCategoryAsync(query.Category, query.IsActive, ct);

        var dtos = items.Select(x => new MasterDataItemDto(
            x.Id,
            x.Category.ToString(),
            x.Code,
            x.DisplayName,
            x.Description,
            x.SortOrder,
            x.IsActive,
            x.CreatedAt)).ToList();

        return Result<List<MasterDataItemDto>>.Success(dtos);
    }
}
