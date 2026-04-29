using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Teams.DTOs;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeamMembers;

internal sealed class GetTeamMembersQueryHandler
    : IQueryHandler<GetTeamMembersQuery, List<TeamMemberDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetTeamMembersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<TeamMemberDto>>> Handle(
        GetTeamMembersQuery query,
        CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;

        if (teamId is null)
            return Result<List<TeamMemberDto>>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var members = await _db.Users
            .AsNoTracking()
            .Where(u => u.TeamId == teamId)
            .Select(u => new TeamMemberDto(
                u.UserId,
                (u.FirstName + " " + u.LastName).Trim(),
                u.Role != null ? u.Role.RoleName : null,
                u.IsActive,
                u.CurrentWorkload,
                u.LastAssignedAt))
            .OrderBy(m => m.FullName)
            .ToListAsync(ct);

        return Result<List<TeamMemberDto>>.Success(members);
    }
}
