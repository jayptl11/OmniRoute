using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.CreateFollowUpTask;

internal sealed class CreateFollowUpTaskCommandHandler
    : ICommandHandler<CreateFollowUpTaskCommand, CreateFollowUpTaskResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IFollowUpTaskRepository _followUpTaskRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateFollowUpTaskCommandHandler(
        ILeadRepository leadRepository,
        IFollowUpTaskRepository followUpTaskRepository,
        IActivityLogRepository activityLogRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _leadRepository = leadRepository;
        _followUpTaskRepository = followUpTaskRepository;
        _activityLogRepository = activityLogRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CreateFollowUpTaskResponse>> Handle(
        CreateFollowUpTaskCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);

        // SA-06: Chỉ đặt follow-up cho lead được gán cho mình
        if (lead is null || lead.AssignedUserId != currentUserId)
            return Result<CreateFollowUpTaskResponse>.Failure(
                "NOT_FOUND", "Lead không tồn tại hoặc chưa được gán cho bạn.");

        var task = FollowUpTask.Create(
            leadId: lead.Id,
            userId: currentUserId,
            dueAt: command.DueAt,
            note: command.Note);

        await _followUpTaskRepository.AddAsync(task, ct);

        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "FOLLOW_UP_SCHEDULED",
            performedBy: currentUserId,
            note: $"Hẹn follow-up lúc {command.DueAt:yyyy-MM-dd HH:mm} UTC: {command.Note}");

        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result<CreateFollowUpTaskResponse>.Success(new CreateFollowUpTaskResponse(
            TaskId: task.Id,
            LeadId: lead.Id,
            DueAt: task.DueAt,
            CreatedAt: task.CreatedAt));
    }
}
