using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Queries.GetAssignedTicketById;

internal sealed class GetAssignedTicketByIdQueryHandler
    : IQueryHandler<GetAssignedTicketByIdQuery, TicketDetailDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetAssignedTicketByIdQueryHandler(
        ITicketRepository ticketRepository,
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TicketDetailDto>> Handle(
        GetAssignedTicketByIdQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        // CS-02: Chỉ lấy ticket đang được gán cho nhân viên CS hiện tại
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.AssignedUser)
            .Where(t => t.Id == query.TicketId && t.AssignedUserId == currentUserId)
            .FirstOrDefaultAsync(ct);

        if (ticket is null)
            return Result<TicketDetailDto>.Failure("NOT_FOUND", "Ticket không tồn tại hoặc chưa được gán cho bạn.");

        string? assignedUserName = null;
        if (ticket.AssignedUser is not null)
        {
            var fullName = $"{ticket.AssignedUser.FirstName} {ticket.AssignedUser.LastName}".Trim();
            assignedUserName = string.IsNullOrWhiteSpace(fullName)
                ? ticket.AssignedUser.Username
                : fullName;
        }

        // Activity timeline (sắp xếp theo thời gian tăng dần)
        var activityLogs = await _db.ActivityLogs
            .AsNoTracking()
            .Include(al => al.PerformedByUser)
            .Where(al => al.EntityType == "TICKET" && al.EntityId == ticket.Id)
            .OrderBy(al => al.PerformedAt)
            .Select(al => new TicketActivityLogItemDto(
                al.Id,
                al.Action,
                al.Note,
                al.NewValue,
                al.PerformedAt,
                al.PerformedByUser == null
                    ? null
                    : ($"{al.PerformedByUser.FirstName} {al.PerformedByUser.LastName}".Trim() == ""
                        ? al.PerformedByUser.Username
                        : $"{al.PerformedByUser.FirstName} {al.PerformedByUser.LastName}".Trim())))
            .ToListAsync(ct);

        // CS-02: Lịch sử ticket trước của cùng số điện thoại
        var history = await _ticketRepository.GetByCustomerPhoneAsync(ticket.CustomerPhone, ct);
        var customerHistory = history
            .Where(t => t.Id != ticket.Id)
            .Select(t => new CustomerTicketHistoryItemDto(
                t.Id,
                t.TicketCode,
                t.NeedType?.ToString(),
                t.Status.ToString(),
                t.CreatedAt,
                t.ClosedAt))
            .ToList();

        var dto = new TicketDetailDto(
            TicketId: ticket.Id,
            TicketCode: ticket.TicketCode,
            CustomerName: ticket.CustomerName,
            CustomerPhone: ticket.CustomerPhone,
            CustomerAddress: ticket.CustomerAddress,
            CustomerEmail: ticket.CustomerEmail,
            Channel: ticket.Channel.ToString(),
            NeedDescription: ticket.NeedDescription,
            NeedType: ticket.NeedType?.ToString(),
            PriorityScore: ticket.PriorityScore,
            PriorityLevel: ticket.PriorityLevel?.ToString(),
            AssignedUserId: ticket.AssignedUserId,
            AssignedUserName: assignedUserName,
            AssignedStoreId: ticket.AssignedStoreId,
            AssignedAt: ticket.AssignedAt,
            SlaDeadline: ticket.SlaDeadline,
            SlaViolated: ticket.SlaViolated,
            TicketStatus: ticket.Status.ToString(),
            IsEscalated: ticket.EscalatedAt.HasValue,
            EscalatedReason: ticket.EscalatedReason,
            SatisfactionScore: ticket.SatisfactionScore,
            SatisfactionNote: ticket.SatisfactionNote,
            CreatedBy: ticket.CreatedBy,
            CreatedAt: ticket.CreatedAt,
            UpdatedAt: ticket.UpdatedAt,
            ClosedAt: ticket.ClosedAt,
            ActivityLogs: activityLogs,
            CustomerTicketHistory: customerHistory);

        return Result<TicketDetailDto>.Success(dto);
    }
}
