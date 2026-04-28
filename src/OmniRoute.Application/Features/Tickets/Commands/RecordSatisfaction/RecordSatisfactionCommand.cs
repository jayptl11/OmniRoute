using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Commands.RecordSatisfaction;

public record RecordSatisfactionCommand(
    Guid TicketId,
    int Score,
    string? Note) : ICommand<RecordSatisfactionResponse>;
