using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.DispatchLeadToStore;

internal sealed class DispatchLeadToStoreCommandHandler
    : ICommandHandler<DispatchLeadToStoreCommand, DispatchLeadToStoreResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly ISlaConfigRepository _slaConfigRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DispatchLeadToStoreCommandHandler(
        ILeadRepository leadRepository,
        IStoreRepository storeRepository,
        ISlaConfigRepository slaConfigRepository,
        IActivityLogRepository activityLogRepository,
        INotificationRepository notificationRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _leadRepository = leadRepository;
        _storeRepository = storeRepository;
        _slaConfigRepository = slaConfigRepository;
        _activityLogRepository = activityLogRepository;
        _notificationRepository = notificationRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<DispatchLeadToStoreResponse>> Handle(
        DispatchLeadToStoreCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);
        if (lead is null || lead.Status != LeadStatus.PendingDispatch)
            return Result<DispatchLeadToStoreResponse>.Failure(
                "NOT_FOUND", "Lead không tồn tại hoặc không ở trạng thái chờ điều phối.");

        var store = await _storeRepository.GetByIdAsync(command.StoreId, ct);
        if (store is null || !store.IsActive)
            return Result<DispatchLeadToStoreResponse>.Failure(
                "STORE_NOT_FOUND", "Cửa hàng không tồn tại hoặc đang không hoạt động.");

        // Tính SLA deadline theo config StoreSupport × priority của lead
        var priority = lead.PriorityLevel ?? PriorityLevel.Low;
        var slaConfig = await _slaConfigRepository.GetByGroupAndPriorityAsync(
            AssignedGroup.StoreSupport, priority, ct);

        var slaDeadline = slaConfig is not null
            ? DateTime.UtcNow.AddHours(slaConfig.MaxHours)
            : DateTime.UtcNow.AddHours(24); // fallback mặc định

        // Domain mutation — guard bên trong entity sẽ throw nếu trạng thái sai
        lead.DispatchToStore(command.StoreId, slaDeadline);
        await _leadRepository.UpdateAsync(lead, ct);

        // Ghi ActivityLog (DP-05 tích hợp: note tùy chọn)
        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "DISPATCHED_TO_STORE",
            performedBy: currentUserId,
            newValue: store.StoreName,
            note: command.Note);
        await _activityLogRepository.AddAsync(log, ct);

        // Gửi notification đến QL (store manager) nếu có
        if (store.ManagerId.HasValue)
        {
            var notification = Notification.Create(
                userId: store.ManagerId.Value,
                type: "STORE_LEAD_ASSIGNED",
                title: $"Lead mới được điều phối về cửa hàng: {lead.LeadCode}",
                body: $"Khách hàng {lead.CustomerName} ({lead.CustomerPhone}) đã được điều phối về {store.StoreName}.",
                entityType: "LEAD",
                entityId: lead.Id);
            await _notificationRepository.AddAsync(notification, ct);
        }

        await _context.SaveChangesAsync(ct);

        return Result<DispatchLeadToStoreResponse>.Success(new DispatchLeadToStoreResponse(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            AssignedStoreId: store.Id,
            StoreName: store.StoreName,
            AssignedAt: lead.AssignedAt!.Value,
            SlaDeadline: slaDeadline));
    }
}
