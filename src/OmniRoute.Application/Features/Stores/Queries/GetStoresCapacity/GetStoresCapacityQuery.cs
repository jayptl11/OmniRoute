using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Stores.DTOs;

namespace OmniRoute.Application.Features.Stores.Queries.GetStoresCapacity;

public record GetStoresCapacityQuery(string? Q) : IQuery<List<StoreCapacityDto>>;
