using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.ReportInvalidLead;

internal sealed class ReportInvalidLeadCommandHandler
    : ICommandHandler<ReportInvalidLeadCommand, ReportInvalidLeadResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReportInvalidLeadCommandHandler(
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        INotificationRepository notificationRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _notificationRepository = notificationRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReportInvalidLeadResponse>> Handle(
        ReportInvalidLeadCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);

        // SA-08: Chỉ xử lý lead được gán cho nhân viên hiện tại
        if (lead is null || lead.AssignedUserId != currentUserId)
            return Result<ReportInvalidLeadResponse>.Failure(
                "NOT_FOUND", "Lead không tồn tại hoặc chưa được gán cho bạn.");

        var oldStatus = lead.Status;

        // SA-08: Áp dụng chuỗi chuyển trạng thái về Cancelled
        // Nếu lead đang ở Contacted/InProgress, cần đảm bảo transition hợp lệ
        try
        {
            // Với Assigned hoặc Contacted hoặc InProgress → tất cả đều có thể Cancelled theo BR-05
            lead.TransitionStatus(LeadStatus.Cancelled);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ReportInvalidLeadResponse>.Failure("INVALID_TRANSITION", ex.Message);
        }

        await _leadRepository.UpdateAsync(lead, ct);

        // Ghi log hành động báo không hợp lệ
        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "INVALID_LEAD_REPORTED",
            performedBy: currentUserId,
            oldValue: oldStatus.ToString(),
            newValue: LeadStatus.Cancelled.ToString(),
            note: command.Reason);

        await _activityLogRepository.AddAsync(log, ct);

        // Gửi notification cho Trưởng nhóm (TN) để review
        var teamId = _currentUserService.TeamId;
        if (teamId.HasValue)
        {
            var leaderId = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == teamId.Value)
                .Select(t => t.LeaderId)
                .FirstOrDefaultAsync(ct);

            if (leaderId.HasValue)
            {
                var notification = Notification.Create(
                    userId: leaderId.Value,
                    type: "INVALID_LEAD",
                    title: "Lead báo không hợp lệ",
                    body: $"Lead {lead.LeadCode} bị báo không hợp lệ. Lý do: {command.Reason}",
                    entityType: "LEAD",
                    entityId: lead.Id);

                await _notificationRepository.AddAsync(notification, ct);
            }
        }

        await _context.SaveChangesAsync(ct);

        return Result<ReportInvalidLeadResponse>.Success(new ReportInvalidLeadResponse(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            CancelledAt: lead.ClosedAt!.Value));
    }
}
