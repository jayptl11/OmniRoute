using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.EscalateLead;

internal sealed class EscalateLeadCommandHandler : ICommandHandler<EscalateLeadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    // Role names có thể nhận escalate từ TN.
    private static readonly HashSet<string> AllowedEscalateTargetRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            RoleCatalog.TeamLead,
            RoleCatalog.StoreManager,
            RoleCatalog.SystemAdmin
        };

    public EscalateLeadCommandHandler(
        IApplicationDbContext db,
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(EscalateLeadCommand command, CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;
        if (teamId is null)
        {
            return Result.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");
        }

        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);
        if (lead is null)
        {
            return Result.Failure("LEAD_NOT_FOUND", "Không tìm thấy lead.");
        }

        // Lead phải thuộc team của TN.
        if (lead.AssignedUserId is null)
        {
            return Result.Failure("LEAD_NOT_ASSIGNED", "Lead này chưa được gán cho nhân viên nào.");
        }

        var assignedUserInTeam = await _db.Users
            .AnyAsync(u => u.UserId == lead.AssignedUserId && u.TeamId == teamId, ct);

        if (!assignedUserInTeam)
        {
            return Result.Failure("LEAD_NOT_IN_TEAM", "Lead này không thuộc đội của bạn.");
        }

        var terminalStatuses = new HashSet<LeadStatus>
        {
            LeadStatus.Won,
            LeadStatus.Lost,
            LeadStatus.Cancelled
        };

        if (terminalStatuses.Contains(lead.Status))
        {
            return Result.Failure("LEAD_TERMINAL", "Không thể escalate lead đã đóng (Won/Lost/Cancelled).");
        }

        // Kiểm tra người nhận: tồn tại, active, role hợp lệ.
        var targetUser = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == command.EscalateTo && u.IsActive, ct);

        if (targetUser is null)
        {
            return Result.Failure("TARGET_NOT_FOUND", "Người nhận escalate không tồn tại hoặc đã bị khóa.");
        }

        if (targetUser.Role is null || !AllowedEscalateTargetRoles.Contains(targetUser.Role.RoleName))
        {
            return Result.Failure(
                "INVALID_TARGET_ROLE",
                $"Chỉ có thể escalate đến {RoleCatalog.GetDisplayName(RoleCatalog.TeamLead)} ({RoleCatalog.TeamLead}), {RoleCatalog.GetDisplayName(RoleCatalog.StoreManager)} ({RoleCatalog.StoreManager}) hoặc {RoleCatalog.GetDisplayName(RoleCatalog.SystemAdmin)} ({RoleCatalog.SystemAdmin}).");
        }

        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "ESCALATED",
            performedBy: currentUserId,
            newValue: command.EscalateTo.ToString(),
            note: command.Reason);
        await _activityLogRepository.AddAsync(log, ct);

        var notification = Notification.Create(
            userId: command.EscalateTo,
            type: "ESCALATED",
            title: $"Lead {lead.LeadCode} được escalate đến bạn",
            body: $"Lý do: {command.Reason}",
            entityType: "LEAD",
            entityId: lead.Id);
        await _notificationRepository.AddAsync(notification, ct);

        return Result.Success();
    }
}
