using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Commands.RecordSatisfaction;

internal sealed class RecordSatisfactionCommandHandler
    : ICommandHandler<RecordSatisfactionCommand, RecordSatisfactionResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RecordSatisfactionCommandHandler(
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

    public async Task<Result<RecordSatisfactionResponse>> Handle(
        RecordSatisfactionCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct);

        if (ticket is null || ticket.AssignedUserId != currentUserId)
            return Result<RecordSatisfactionResponse>.Failure(
                "NOT_FOUND", "Ticket không tồn tại hoặc chưa được gán cho bạn.");

        try
        {
            ticket.RecordSatisfaction(command.Score, command.Note);
        }
        catch (InvalidOperationException ex)
        {
            return Result<RecordSatisfactionResponse>.Failure("INVALID_STATUS", ex.Message);
        }

        await _ticketRepository.UpdateAsync(ticket, ct);

        var log = ActivityLog.Create(
            entityType: "TICKET",
            entityId: ticket.Id,
            action: "SATISFACTION_RECORDED",
            performedBy: currentUserId,
            newValue: command.Score.ToString(),
            note: command.Note);

        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result<RecordSatisfactionResponse>.Success(new RecordSatisfactionResponse(
            TicketId: ticket.Id,
            TicketCode: ticket.TicketCode,
            SatisfactionScore: command.Score,
            UpdatedAt: ticket.UpdatedAt));
    }
}
