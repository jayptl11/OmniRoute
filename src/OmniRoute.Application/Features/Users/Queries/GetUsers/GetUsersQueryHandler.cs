using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.DTOs;

namespace OmniRoute.Application.Features.Users.Queries.GetUsers;

internal sealed class GetUsersQueryHandler
    : IQueryHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<UserListItemDto>>> Handle(
        GetUsersQuery query,
        CancellationToken ct)
    {
        var q = _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.RoleName))
            q = q.Where(u => u.Role != null && u.Role.RoleName == query.RoleName);

        if (query.StoreId.HasValue)
            q = q.Where(u => u.StoreId == query.StoreId);

        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserListItemDto(
                u.UserId,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role != null ? u.Role.RoleName : null,
                u.RoleId,
                u.StoreId,
                u.IsActive,
                u.LastLogin,
                u.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<UserListItemDto>>.Success(
            new PagedResult<UserListItemDto>(items, totalCount, query.Page, query.PageSize));
    }
}
