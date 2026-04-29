using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Application.Features.Teams.Commands.AddTeamMember;

internal sealed class AddTeamMemberCommandHandler : ICommandHandler<AddTeamMemberCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AddTeamMemberCommandHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AddTeamMemberCommand command, CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;

        if (teamId is null)
            return Result.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        if (!user.IsActive)
            return Result.Failure("USER_INACTIVE", "Người dùng đã bị khóa, không thể thêm vào đội.");

        if (user.TeamId == teamId)
            return Result.Failure("ALREADY_IN_TEAM", "Người dùng đã là thành viên của đội này.");

        if (user.TeamId is not null && user.TeamId != teamId)
            return Result.Failure("IN_OTHER_TEAM", "Người dùng đang thuộc đội khác. Hãy xóa khỏi đội đó trước.");

        user.AssignToTeam(teamId);

        var log = ActivityLog.Create(
            entityType: "USER",
            entityId: command.UserId,
            action: "TEAM_MEMBER_ADDED",
            performedBy: _currentUserService.GetUserId(),
            newValue: teamId.ToString());

        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
