using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Commands.UpdateTicketStatus;

internal sealed class UpdateTicketStatusCommandHandler
    : ICommandHandler<UpdateTicketStatusCommand, UpdateTicketStatusResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTicketStatusCommandHandler(
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

    public async Task<Result<UpdateTicketStatusResponse>> Handle(
        UpdateTicketStatusCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct);

        // CS-04: Chỉ xử lý ticket đang được gán cho nhân viên CS hiện tại
        if (ticket is null || ticket.AssignedUserId != currentUserId)
            return Result<UpdateTicketStatusResponse>.Failure(
                "NOT_FOUND", "Ticket không tồn tại hoặc chưa được gán cho bạn.");

        if (!Enum.TryParse<TicketStatus>(command.NewStatus, ignoreCase: true, out var newStatus))
            return Result<UpdateTicketStatusResponse>.Failure(
                "INVALID_STATUS", $"Trạng thái '{command.NewStatus}' không hợp lệ.");

        var oldStatus = ticket.Status;

        try
        {
            ticket.TransitionStatus(newStatus);
        }
        catch (InvalidOperationException ex)
        {
            return Result<UpdateTicketStatusResponse>.Failure("INVALID_TRANSITION", ex.Message);
        }

        await _ticketRepository.UpdateAsync(ticket, ct);

        var contextNote = newStatus switch
        {
            TicketStatus.InProgress      => command.Note,
            TicketStatus.Resolved        => command.Note,
            TicketStatus.WaitingCustomer => command.Note,
            TicketStatus.Closed          => command.CancelReason ?? command.Note,
            _                            => command.Note
        };

        var log = ActivityLog.Create(
            entityType: "TICKET",
            entityId: ticket.Id,
            action: "STATUS_CHANGED",
            performedBy: currentUserId,
            oldValue: oldStatus.ToString(),
            newValue: newStatus.ToString(),
            note: contextNote);

        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result<UpdateTicketStatusResponse>.Success(new UpdateTicketStatusResponse(
            TicketId: ticket.Id,
            TicketCode: ticket.TicketCode,
            NewStatus: ticket.Status.ToString(),
            UpdatedAt: ticket.UpdatedAt));
    }
}
