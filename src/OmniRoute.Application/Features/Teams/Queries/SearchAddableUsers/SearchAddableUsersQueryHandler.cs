using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Queries.SearchAddableUsers;

internal sealed class SearchAddableUsersQueryHandler
    : IQueryHandler<SearchAddableUsersQuery, List<AddableUserDto>>
{
    private static readonly Dictionary<AssignedGroup, string> AllowedRoleByTeamType = new()
    {
        { AssignedGroup.Sale, RoleCatalog.Sales },
        { AssignedGroup.Cskh, RoleCatalog.CustomerService },
        { AssignedGroup.StoreSupport, RoleCatalog.Dispatcher },
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public SearchAddableUsersQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<AddableUserDto>>> Handle(
        SearchAddableUsersQuery query,
        CancellationToken ct)
    {
        var currentTeamId = _currentUserService.TeamId;

        if (currentTeamId is null)
        {
            return Result<List<AddableUserDto>>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");
        }

        var team = await _db.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == currentTeamId, ct);

        if (team is null)
        {
            return Result<List<AddableUserDto>>.Failure("NO_TEAM", "Đội của bạn không tồn tại.");
        }

        // Chỉ hiển thị role phù hợp với loại đội.
        var allowedRole = AllowedRoleByTeamType.TryGetValue(team.TeamType, out var roleCode)
            ? roleCode
            : null;

        var usersQuery = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.UserId != _currentUserService.GetUserId() && u.IsActive);

        if (allowedRole is not null)
        {
            usersQuery = usersQuery.Where(u => u.Role != null && u.Role.RoleName == allowedRole);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                (u.LastName + " " + u.FirstName).ToLower().Contains(term));
        }

        var users = await usersQuery
            .OrderBy(u => u.TeamId == null ? 0 : (u.TeamId == currentTeamId ? 1 : 2))
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(30)
            .Select(u => new
            {
                u.UserId,
                FullName = ($"{u.FirstName} {u.LastName}".Trim() != string.Empty
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : u.Username),
                u.Username,
                RoleName = u.Role != null ? u.Role.RoleName : null,
                HasTeam = u.TeamId != null && u.TeamId != currentTeamId
            })
            .ToListAsync(ct);

        return Result<List<AddableUserDto>>.Success(
            users.Select(u => new AddableUserDto(
                u.UserId,
                u.FullName,
                u.Username,
                u.RoleName,
                RoleCatalog.GetDisplayName(u.RoleName),
                u.HasTeam))
            .ToList());
    }
}
