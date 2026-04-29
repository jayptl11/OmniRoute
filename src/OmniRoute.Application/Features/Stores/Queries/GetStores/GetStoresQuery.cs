using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Stores.DTOs;

namespace OmniRoute.Application.Features.Stores.Queries.GetStores;

public record GetStoresQuery(string? Search, string? Region, bool? IsActive) : IQuery<List<StoreDto>>;
