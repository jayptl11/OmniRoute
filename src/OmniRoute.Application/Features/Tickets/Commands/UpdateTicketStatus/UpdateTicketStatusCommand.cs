using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Commands.UpdateTicketStatus;

public record UpdateTicketStatusCommand(
    Guid TicketId,
    string NewStatus,
    string? Note,
    string? CancelReason) : ICommand<UpdateTicketStatusResponse>;
