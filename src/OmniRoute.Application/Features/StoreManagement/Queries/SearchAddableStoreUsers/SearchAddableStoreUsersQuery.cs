using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.SearchAddableStoreUsers;

public record SearchAddableStoreUsersQuery(string? Search) : IQuery<List<AddableStoreUserDto>>;
