using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.UpdateLeadStatus;

internal sealed class UpdateLeadStatusCommandHandler
    : ICommandHandler<UpdateLeadStatusCommand, UpdateLeadStatusResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateLeadStatusCommandHandler(
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UpdateLeadStatusResponse>> Handle(
        UpdateLeadStatusCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);

        // SA-04: Chỉ xử lý lead đang được gán cho nhân viên hiện tại
        if (lead is null || lead.AssignedUserId != currentUserId)
            return Result<UpdateLeadStatusResponse>.Failure(
                "NOT_FOUND", "Lead không tồn tại hoặc chưa được gán cho bạn.");

        if (!Enum.TryParse<LeadStatus>(command.NewStatus, ignoreCase: true, out var newStatus))
            return Result<UpdateLeadStatusResponse>.Failure(
                "INVALID_STATUS", $"Trạng thái '{command.NewStatus}' không hợp lệ.");

        var oldStatus = lead.Status;

        try
        {
            lead.TransitionStatus(newStatus);
        }
        catch (InvalidOperationException ex)
        {
            return Result<UpdateLeadStatusResponse>.Failure("INVALID_TRANSITION", ex.Message);
        }

        await _leadRepository.UpdateAsync(lead, ct);

        // Xây dựng note ngữ cảnh theo trạng thái đích
        var contextNote = newStatus switch
        {
            LeadStatus.Contacted  => command.Note,
            LeadStatus.InProgress => command.Note,
            LeadStatus.Won        => command.WonDetails,
            LeadStatus.Lost       => command.LostReason,
            LeadStatus.Cancelled  => command.CancelReason,
            _                     => command.Note
        };

        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "STATUS_CHANGED",
            performedBy: currentUserId,
            oldValue: oldStatus.ToString(),
            newValue: newStatus.ToString(),
            note: contextNote);

        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result<UpdateLeadStatusResponse>.Success(new UpdateLeadStatusResponse(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            NewStatus: lead.Status.ToString(),
            UpdatedAt: lead.UpdatedAt));
    }
}
