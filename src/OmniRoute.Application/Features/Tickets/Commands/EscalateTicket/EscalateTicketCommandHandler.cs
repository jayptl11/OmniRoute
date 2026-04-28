using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Commands.EscalateTicket;

internal sealed class EscalateTicketCommandHandler
    : ICommandHandler<EscalateTicketCommand, EscalateTicketResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public EscalateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IActivityLogRepository activityLogRepository,
        INotificationRepository notificationRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _activityLogRepository = activityLogRepository;
        _notificationRepository = notificationRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<EscalateTicketResponse>> Handle(
        EscalateTicketCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct);

        if (ticket is null || ticket.AssignedUserId != currentUserId)
            return Result<EscalateTicketResponse>.Failure(
                "NOT_FOUND", "Ticket không tồn tại hoặc chưa được gán cho bạn.");

        // Kiểm tra người nhận escalate tồn tại
        var targetUserExists = await _context.Users
            .AnyAsync(u => u.UserId == command.EscalateTo && u.IsActive, ct);

        if (!targetUserExists)
            return Result<EscalateTicketResponse>.Failure(
                "INVALID_TARGET", "Người nhận escalate không tồn tại hoặc đã bị khóa.");

        try
        {
            ticket.Escalate(command.EscalateTo, command.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result<EscalateTicketResponse>.Failure("INVALID_TRANSITION", ex.Message);
        }

        await _ticketRepository.UpdateAsync(ticket, ct);

        var log = ActivityLog.Create(
            entityType: "TICKET",
            entityId: ticket.Id,
            action: "ESCALATED",
            performedBy: currentUserId,
            newValue: command.EscalateTo.ToString(),
            note: command.Reason);

        await _activityLogRepository.AddAsync(log, ct);

        // Gửi notification đến người nhận escalate
        var notification = Notification.Create(
            userId: command.EscalateTo,
            type: "ESCALATED",
            title: $"Ticket {ticket.TicketCode} được escalate đến bạn",
            body: $"Lý do: {command.Reason}",
            entityType: "TICKET",
            entityId: ticket.Id);

        await _notificationRepository.AddAsync(notification, ct);
        await _context.SaveChangesAsync(ct);

        return Result<EscalateTicketResponse>.Success(new EscalateTicketResponse(
            TicketId: ticket.Id,
            TicketCode: ticket.TicketCode,
            EscalatedTo: command.EscalateTo,
            EscalatedAt: ticket.EscalatedAt!.Value));
    }
}
