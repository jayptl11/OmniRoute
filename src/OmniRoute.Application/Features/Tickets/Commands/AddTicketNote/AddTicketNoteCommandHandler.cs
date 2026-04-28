using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Commands.AddTicketNote;

internal sealed class AddTicketNoteCommandHandler
    : ICommandHandler<AddTicketNoteCommand, AddTicketNoteResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddTicketNoteCommandHandler(
        ITicketRepository ticketRepository,
        IActivityLogRepository activityLogRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _activityLogRepository = activityLogRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AddTicketNoteResponse>> Handle(
        AddTicketNoteCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct);

        // CS-05: Chỉ ghi chú cho ticket đang được gán cho nhân viên CS hiện tại
        if (ticket is null || ticket.AssignedUserId != currentUserId)
            return Result<AddTicketNoteResponse>.Failure(
                "NOT_FOUND", "Ticket không tồn tại hoặc chưa được gán cho bạn.");

        var log = ActivityLog.Create(
            entityType: "TICKET",
            entityId: ticket.Id,
            action: "PROCESSING_NOTE",
            performedBy: currentUserId,
            note: command.Content);

        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result<AddTicketNoteResponse>.Success(new AddTicketNoteResponse(
            NoteId: log.Id,
            TicketId: ticket.Id,
            CreatedAt: log.PerformedAt));
    }
}
