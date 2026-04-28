using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Tickets.Queries.GetAssignedTickets;

internal sealed class GetAssignedTicketsQueryHandler
    : IQueryHandler<GetAssignedTicketsQuery, PagedResult<TicketListItemDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAssignedTicketsQueryHandler(
        ITicketRepository ticketRepository,
        ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<TicketListItemDto>>> Handle(
        GetAssignedTicketsQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var (tickets, totalCount) = await _ticketRepository.GetAssignedTicketsAsync(
            assignedUserId: currentUserId,
            search: query.Search,
            status: query.Status,
            priorityLevel: query.PriorityLevel,
            dateFrom: query.DateFrom,
            dateTo: query.DateTo,
            page: query.Page,
            pageSize: query.PageSize,
            ct: ct);

        var items = tickets.Select(t => new TicketListItemDto(
            TicketId: t.Id,
            TicketCode: t.TicketCode,
            CustomerName: t.CustomerName,
            CustomerPhone: t.CustomerPhone,
            NeedType: t.NeedType?.ToString(),
            TicketStatus: t.Status.ToString(),
            PriorityLevel: t.PriorityLevel?.ToString(),
            SlaDeadline: t.SlaDeadline,
            SlaViolated: t.SlaViolated,
            AssignedAt: t.AssignedAt)).ToList();

        return Result<PagedResult<TicketListItemDto>>.Success(
            new PagedResult<TicketListItemDto>(items, totalCount, query.Page, query.PageSize));
    }
}
