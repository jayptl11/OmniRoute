using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Leads.Commands.ReassignLead;

public record ReassignLeadCommand(Guid LeadId, Guid NewUserId, string Reason) : ICommand;
