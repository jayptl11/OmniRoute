using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Commands.RemoveTeamMember;

internal sealed class RemoveTeamMemberCommandHandler : ICommandHandler<RemoveTeamMemberCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public RemoveTeamMemberCommandHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RemoveTeamMemberCommand command, CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;

        if (teamId is null)
            return Result.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        if (user.TeamId != teamId)
            return Result.Failure("USER_NOT_IN_TEAM", "Người dùng không thuộc đội của bạn.");

        var terminalStatuses = new[]
        {
            LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled
        };

        var activeLeadCount = await _db.Leads
            .CountAsync(l => l.AssignedUserId == command.UserId && !terminalStatuses.Contains(l.Status), ct);

        if (activeLeadCount > 0)
            return Result.Failure(
                "ACTIVE_LEADS_WARNING",
                $"Người dùng đang có {activeLeadCount} lead chưa hoàn tất. Hãy chuyển giao (reassign) trước khi xóa khỏi đội.");

        user.AssignToTeam(null);

        var log = ActivityLog.Create(
            entityType: "USER",
            entityId: command.UserId,
            action: "TEAM_MEMBER_REMOVED",
            performedBy: _currentUserService.GetUserId(),
            oldValue: teamId.ToString());

        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
