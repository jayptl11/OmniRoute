using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.SearchAddableStoreUsers;

internal sealed class SearchAddableStoreUsersQueryHandler
    : IQueryHandler<SearchAddableStoreUsersQuery, List<AddableStoreUserDto>>
{
    // QL có thể thêm nhân viên SA, CS, DP vào đơn vị
    private static readonly HashSet<string> AllowedRoles = ["SA", "CS", "DP"];

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
            return Result<List<AddableStoreUserDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var q = _db.Users
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
            q = q.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                (u.LastName + " " + u.FirstName).ToLower().Contains(term));
        }

        var users = await q
            .OrderBy(u => u.StoreId == null ? 0 : (u.StoreId == storeId ? 1 : 2))
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(30)
            .Select(u => new AddableStoreUserDto(
                u.UserId,
                (u.FirstName + " " + u.LastName).Trim() != string.Empty
                    ? (u.FirstName + " " + u.LastName).Trim()
                    : u.Username,
                u.Username,
                u.Role != null ? u.Role.RoleName : null,
                u.StoreId != null && u.StoreId != storeId))
            .ToListAsync(ct);

        return Result<List<AddableStoreUserDto>>.Success(users);
    }
}
