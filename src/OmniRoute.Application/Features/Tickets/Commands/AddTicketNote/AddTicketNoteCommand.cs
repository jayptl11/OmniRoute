using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Commands.AddTicketNote;

public record AddTicketNoteCommand(Guid TicketId, string Content) : ICommand<AddTicketNoteResponse>;
