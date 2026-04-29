using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetEscalateTargets;

internal sealed class GetEscalateTargetsQueryHandler
    : IQueryHandler<GetEscalateTargetsQuery, List<EscalateTargetDto>>
{
    private static readonly HashSet<string> SearchRoles =
        new(StringComparer.OrdinalIgnoreCase) { "TN", "QL", "QT" };

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
        var q = query.Q?.Trim();
        var hasQuery = !string.IsNullOrEmpty(q);

        var targets = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u =>
                u.IsActive &&
                u.UserId != currentUserId &&
                u.Role != null &&
                // Không nhập → chỉ TN; có nhập → search TN/QL/QT
                (hasQuery
                    ? SearchRoles.Contains(u.Role.RoleName)
                    : u.Role.RoleName == "TN") &&
                (!hasQuery || (
                    u.Username.Contains(q!) ||
                    (u.FirstName + " " + u.LastName).Contains(q!) ||
                    u.LastName.Contains(q!) ||
                    u.FirstName.Contains(q!))))
            .OrderBy(u => u.Role!.RoleName)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new EscalateTargetDto(
                u.UserId,
                ($"{u.FirstName} {u.LastName}".Trim() != string.Empty
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : u.Username),
                u.Role!.RoleName))
            .ToListAsync(ct);

        return Result<List<EscalateTargetDto>>.Success(targets);
    }
}
