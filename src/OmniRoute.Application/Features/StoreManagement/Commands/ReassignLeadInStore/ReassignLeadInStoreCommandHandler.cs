using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.StoreManagement.Commands.ReassignLeadInStore;

internal sealed class ReassignLeadInStoreCommandHandler : ICommandHandler<ReassignLeadInStoreCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ISlaConfigRepository _slaConfigRepository;
    private readonly ICurrentUserService _currentUserService;

    public ReassignLeadInStoreCommandHandler(
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

    public async Task<Result> Handle(ReassignLeadInStoreCommand command, CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
            return Result.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);

        if (lead is null)
            return Result.Failure("LEAD_NOT_FOUND", "Không tìm thấy lead.");

        if (lead.AssignedStoreId != storeId)
            return Result.Failure("LEAD_NOT_IN_STORE", "Lead này không thuộc đơn vị của bạn.");

        var terminalStatuses = new HashSet<LeadStatus>
            { LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };

        if (terminalStatuses.Contains(lead.Status))
            return Result.Failure("LEAD_TERMINAL", "Không thể reassign lead đã đóng (Won/Lost/Cancelled).");

        // Validate new user belongs to this store and is active
        var newUser = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == command.NewUserId && u.StoreId == storeId && u.IsActive, ct);

        if (newUser is null)
            return Result.Failure("NEW_USER_NOT_FOUND", "Nhân viên mới không tồn tại, không thuộc đơn vị hoặc đã bị khóa.");

        if (newUser.UserId == lead.AssignedUserId)
            return Result.Failure("SAME_USER", "Lead đã được gán cho nhân viên này rồi.");

        // Recalculate SLA deadline from NOW (BR-03)
        var priorityLevel = lead.PriorityLevel ?? PriorityLevel.Low;
        var assignedGroup = lead.AssignedGroup ?? AssignedGroup.Sale;
        var slaConfig = await _slaConfigRepository.GetByGroupAndPriorityAsync(assignedGroup, priorityLevel, ct);
        var newSlaDeadline = DateTime.UtcNow.AddHours(slaConfig?.MaxHours ?? 8);

        var oldUserId = lead.AssignedUserId;

        lead.AssignToUser(command.NewUserId, newSlaDeadline);
        await _leadRepository.UpdateAsync(lead, ct);

        // Update workloads
        var oldUser = oldUserId.HasValue
            ? await _db.Users.FirstOrDefaultAsync(u => u.UserId == oldUserId.Value, ct)
            : null;

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

        // Notification to new user
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
