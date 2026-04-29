using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Leads.Commands.AddInternalNote;

public record AddInternalNoteToLeadCommand(Guid LeadId, string Content) : ICommand;
