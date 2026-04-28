using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Queries.GetAssignedTickets;

public record GetAssignedTicketsQuery(
    string? Search,
    string? Status,
    string? PriorityLevel,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TicketListItemDto>>;
