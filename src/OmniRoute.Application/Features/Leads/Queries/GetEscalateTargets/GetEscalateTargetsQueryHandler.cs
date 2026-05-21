using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Constants;

namespace OmniRoute.Application.Features.Leads.Queries.GetEscalateTargets;

internal sealed class GetEscalateTargetsQueryHandler
    : IQueryHandler<GetEscalateTargetsQuery, List<EscalateTargetDto>>
{
    private static readonly HashSet<string> SearchRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            RoleCatalog.TeamLead,
            RoleCatalog.StoreManager,
            RoleCatalog.SystemAdmin
        };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetEscalateTargetsQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<EscalateTargetDto>>> Handle(
        GetEscalateTargetsQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();
        var searchText = query.Q?.Trim();
        var hasQuery = !string.IsNullOrEmpty(searchText);

        var targets = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u =>
                u.IsActive &&
                u.UserId != currentUserId &&
                u.Role != null &&
                // Không nhập -> chỉ TN; có nhập -> search TN/QL/QT.
                (hasQuery
                    ? SearchRoles.Contains(u.Role.RoleName)
                    : u.Role.RoleName == RoleCatalog.TeamLead) &&
                (!hasQuery || (
                    u.Username.Contains(searchText!) ||
                    ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Contains(searchText!) ||
                    (u.LastName ?? string.Empty).Contains(searchText!) ||
                    (u.FirstName ?? string.Empty).Contains(searchText!))))
            .OrderBy(u => u.Role!.RoleName)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new
            {
                u.UserId,
                FullName = ($"{u.FirstName ?? string.Empty} {u.LastName ?? string.Empty}".Trim() != string.Empty
                    ? $"{u.FirstName ?? string.Empty} {u.LastName ?? string.Empty}".Trim()
                    : u.Username),
                RoleName = u.Role!.RoleName
            })
            .ToListAsync(ct);

        return Result<List<EscalateTargetDto>>.Success(
            targets.Select(u => new EscalateTargetDto(
                u.UserId,
                u.FullName,
                u.RoleName,
                RoleCatalog.GetDisplayName(u.RoleName) ?? u.RoleName))
            .ToList());
    }
}
