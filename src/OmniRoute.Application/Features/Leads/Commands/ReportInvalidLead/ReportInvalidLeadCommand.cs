using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Commands.ReportInvalidLead;

public record ReportInvalidLeadCommand(Guid LeadId, string Reason) : ICommand<ReportInvalidLeadResponse>;
