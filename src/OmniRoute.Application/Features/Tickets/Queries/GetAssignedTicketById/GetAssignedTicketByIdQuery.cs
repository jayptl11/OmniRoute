using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Queries.GetAssignedTicketById;

public record GetAssignedTicketByIdQuery(Guid TicketId) : IQuery<TicketDetailDto>;
