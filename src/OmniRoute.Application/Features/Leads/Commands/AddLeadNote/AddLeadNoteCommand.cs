using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Commands.AddLeadNote;

public record AddLeadNoteCommand(Guid LeadId, string Content) : ICommand<AddLeadNoteResponse>;
