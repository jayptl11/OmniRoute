using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Stores.DTOs;

namespace OmniRoute.Application.Features.Stores.Queries.GetStoreById;

public record GetStoreByIdQuery(Guid Id) : IQuery<StoreDto>;
