using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Commands.EscalateTicket;

public record EscalateTicketCommand(
    Guid TicketId,
    Guid EscalateTo,
    string Reason) : ICommand<EscalateTicketResponse>;
