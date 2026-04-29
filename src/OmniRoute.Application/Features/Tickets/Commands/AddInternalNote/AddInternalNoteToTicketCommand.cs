using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Tickets.Commands.AddInternalNote;

public record AddInternalNoteToTicketCommand(Guid TicketId, string Content) : ICommand;
