using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;
using OmniRoute.Domain.Constants;

namespace OmniRoute.Application.Features.StoreManagement.Queries.SearchAddableStoreUsers;

internal sealed class SearchAddableStoreUsersQueryHandler
    : IQueryHandler<SearchAddableStoreUsersQuery, List<AddableStoreUserDto>>
{
    // QL chỉ có thể thêm nhân viên sale cửa hàng vào đơn vị.
    private static readonly HashSet<string> AllowedRoles = [RoleCatalog.StoreSales];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public SearchAddableStoreUsersQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<AddableStoreUserDto>>> Handle(
        SearchAddableStoreUsersQuery query,
        CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
        {
            return Result<List<AddableStoreUserDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");
        }

        var usersQuery = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u =>
                u.UserId != _currentUserService.GetUserId() &&
                u.IsActive &&
                u.Role != null &&
                AllowedRoles.Contains(u.Role.RoleName));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                (u.LastName + " " + u.FirstName).ToLower().Contains(term));
        }

        var users = await usersQuery
            .OrderBy(u => u.StoreId == null ? 0 : (u.StoreId == storeId ? 1 : 2))
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(30)
            .Select(u => new
            {
                u.UserId,
                FullName = (u.FirstName + " " + u.LastName).Trim() != string.Empty
                    ? (u.FirstName + " " + u.LastName).Trim()
                    : u.Username,
                u.Username,
                RoleName = u.Role != null ? u.Role.RoleName : null,
                HasStore = u.StoreId != null && u.StoreId != storeId
            })
            .ToListAsync(ct);

        return Result<List<AddableStoreUserDto>>.Success(
            users.Select(u => new AddableStoreUserDto(
                u.UserId,
                u.FullName,
                u.Username,
                u.RoleName,
                RoleCatalog.GetDisplayName(u.RoleName),
                u.HasStore))
            .ToList());
    }
}
