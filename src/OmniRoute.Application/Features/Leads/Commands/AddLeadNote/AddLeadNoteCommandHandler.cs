using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.AddLeadNote;

internal sealed class AddLeadNoteCommandHandler
    : ICommandHandler<AddLeadNoteCommand, AddLeadNoteResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddLeadNoteCommandHandler(
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

    public async Task<Result<AddLeadNoteResponse>> Handle(
        AddLeadNoteCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);

        // SA-05: Chỉ ghi chú cho lead đang được gán cho nhân viên hiện tại
        if (lead is null || lead.AssignedUserId != currentUserId)
            return Result<AddLeadNoteResponse>.Failure(
                "NOT_FOUND", "Lead không tồn tại hoặc chưa được gán cho bạn.");

        var note = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "CONSULTATION_NOTE",
            performedBy: currentUserId,
            note: command.Content);

        await _activityLogRepository.AddAsync(note, ct);
        await _context.SaveChangesAsync(ct);

        return Result<AddLeadNoteResponse>.Success(new AddLeadNoteResponse(
            NoteId: note.Id,
            LeadId: lead.Id,
            CreatedAt: note.PerformedAt));
    }
}
