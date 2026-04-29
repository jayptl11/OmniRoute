using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Stores.DTOs;

namespace OmniRoute.Application.Features.Stores.Queries.SearchStoreManagers;

public record SearchStoreManagersQuery(string? Q) : IQuery<List<StoreManagerDto>>;
