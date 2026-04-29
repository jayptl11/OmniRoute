using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Commands.AddTeamMember;

internal sealed class AddTeamMemberCommandHandler : ICommandHandler<AddTeamMemberCommand>
{
    // Role được phép thêm tương ứng với từng loại đội
    private static readonly Dictionary<AssignedGroup, string> AllowedRoleByTeamType = new()
    {
        { AssignedGroup.Sale,         "SA" },
        { AssignedGroup.Cskh,         "CS" },
        { AssignedGroup.StoreSupport, "DP" },
    };

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

        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return Result.Failure("NO_TEAM", "Đội của bạn không tồn tại.");

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        if (!user.IsActive)
            return Result.Failure("USER_INACTIVE", "Người dùng đã bị khóa, không thể thêm vào đội.");

        if (AllowedRoleByTeamType.TryGetValue(team.TeamType, out var allowedRole))
        {
            var userRole = user.Role?.RoleName;
            if (!string.Equals(userRole, allowedRole, StringComparison.OrdinalIgnoreCase))
                return Result.Failure("INVALID_ROLE",
                    $"Đội loại '{team.TeamType}' chỉ nhận thành viên có role '{allowedRole}'. Người dùng này có role '{userRole ?? "không xác định"}'.");
        }

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
