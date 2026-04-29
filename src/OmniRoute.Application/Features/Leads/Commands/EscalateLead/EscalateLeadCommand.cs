using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Leads.Commands.EscalateLead;

public record EscalateLeadCommand(Guid LeadId, Guid EscalateTo, string Reason) : ICommand;
