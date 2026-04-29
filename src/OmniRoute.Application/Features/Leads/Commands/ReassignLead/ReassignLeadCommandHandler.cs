using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.ReassignLead;

internal sealed class ReassignLeadCommandHandler : ICommandHandler<ReassignLeadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ISlaConfigRepository _slaConfigRepository;
    private readonly ICurrentUserService _currentUserService;

    public ReassignLeadCommandHandler(
        IApplicationDbContext db,
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        INotificationRepository notificationRepository,
        ISlaConfigRepository slaConfigRepository,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _notificationRepository = notificationRepository;
        _slaConfigRepository = slaConfigRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ReassignLeadCommand command, CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;
        if (teamId is null)
            return Result.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);
        if (lead is null)
            return Result.Failure("LEAD_NOT_FOUND", "Không tìm thấy lead.");

        // Kiểm tra lead thuộc team TN (người được gán phải là thành viên đội TN)
        if (lead.AssignedUserId is null)
            return Result.Failure("LEAD_NOT_ASSIGNED", "Lead này chưa được gán cho nhân viên nào.");

        var assignedUserInTeam = await _db.Users
            .AnyAsync(u => u.UserId == lead.AssignedUserId && u.TeamId == teamId, ct);

        if (!assignedUserInTeam)
            return Result.Failure("LEAD_NOT_IN_TEAM", "Lead này không thuộc đội của bạn.");

        // Kiểm tra trạng thái không phải terminal
        var terminalStatuses = new HashSet<LeadStatus>
            { LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };

        if (terminalStatuses.Contains(lead.Status))
            return Result.Failure("LEAD_TERMINAL", "Không thể reassign lead đã đóng (Won/Lost/Cancelled).");

        // Kiểm tra NewUser thuộc team, đang active
        var newUser = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == command.NewUserId && u.TeamId == teamId && u.IsActive, ct);

        if (newUser is null)
            return Result.Failure("NEW_USER_NOT_FOUND", "Nhân viên mới không tồn tại, không thuộc đội hoặc đã bị khóa.");

        if (newUser.UserId == lead.AssignedUserId)
            return Result.Failure("SAME_USER", "Lead đã được gán cho nhân viên này rồi.");

        // Tính lại SLA deadline từ NOW (BR-03: SLA tính từ assigned_at)
        var priorityLevel = lead.PriorityLevel ?? PriorityLevel.Low;
        var assignedGroup = lead.AssignedGroup ?? AssignedGroup.Sale;
        var slaConfig = await _slaConfigRepository.GetByGroupAndPriorityAsync(assignedGroup, priorityLevel, ct);
        int maxHours = slaConfig?.MaxHours ?? 8;
        var newSlaDeadline = DateTime.UtcNow.AddHours(maxHours);

        var oldUserId = lead.AssignedUserId;

        // Reassign lead
        lead.AssignToUser(command.NewUserId, newSlaDeadline);
        await _leadRepository.UpdateAsync(lead, ct);

        // Cập nhật workload
        var oldUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == oldUserId, ct);
        oldUser?.DecrementWorkload();
        newUser.IncrementWorkload();
        newUser.UpdateLastAssigned();
        await _db.SaveChangesAsync(ct);

        // ActivityLog
        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "REASSIGNED",
            performedBy: currentUserId,
            oldValue: oldUserId.ToString(),
            newValue: command.NewUserId.ToString(),
            note: command.Reason);
        await _activityLogRepository.AddAsync(log, ct);

        // Notification đến nhân viên mới
        var notification = Notification.Create(
            userId: command.NewUserId,
            type: "NEW_LEAD",
            title: $"Lead {lead.LeadCode} được reassign đến bạn",
            body: $"Lý do: {command.Reason}",
            entityType: "LEAD",
            entityId: lead.Id);
        await _notificationRepository.AddAsync(notification, ct);

        return Result.Success();
    }
}
