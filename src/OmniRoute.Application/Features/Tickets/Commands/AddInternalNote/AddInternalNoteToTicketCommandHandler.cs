using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Commands.AddInternalNote;

internal sealed class AddInternalNoteToTicketCommandHandler : ICommandHandler<AddInternalNoteToTicketCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ITicketRepository _ticketRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddInternalNoteToTicketCommandHandler(
        IApplicationDbContext db,
        ITicketRepository ticketRepository,
        IActivityLogRepository activityLogRepository,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _ticketRepository = ticketRepository;
        _activityLogRepository = activityLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AddInternalNoteToTicketCommand command, CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;
        if (teamId is null)
            return Result.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct);
        if (ticket is null)
            return Result.Failure("TICKET_NOT_FOUND", "Không tìm thấy ticket.");

        // Ticket phải thuộc team của TN (được gán cho thành viên trong đội TN)
        bool isInScope;
        if (ticket.AssignedUserId.HasValue)
        {
            isInScope = await _db.Users
                .AnyAsync(u => u.UserId == ticket.AssignedUserId && u.TeamId == teamId, ct);
        }
        else
        {
            isInScope = await _db.Users
                .AnyAsync(u => u.UserId == ticket.CreatedBy && u.TeamId == teamId, ct);
        }

        if (!isInScope)
            return Result.Failure("TICKET_NOT_IN_TEAM", "Ticket này không thuộc phạm vi đội của bạn.");

        var log = ActivityLog.Create(
            entityType: "TICKET",
            entityId: ticket.Id,
            action: "INTERNAL_NOTE",
            performedBy: _currentUserService.GetUserId(),
            note: command.Content,
            isInternal: true);

        await _activityLogRepository.AddAsync(log, ct);

        return Result.Success();
    }
}
