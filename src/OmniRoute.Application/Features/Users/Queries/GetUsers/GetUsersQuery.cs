using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.DTOs;

namespace OmniRoute.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(
    string? RoleName,
    Guid? StoreId,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<UserListItemDto>>;
