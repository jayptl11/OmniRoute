using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.MasterData.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.MasterData.Queries.GetMasterDataItems;

public record GetMasterDataItemsQuery(MasterDataCategory Category, bool? IsActive) : IQuery<List<MasterDataItemDto>>;
