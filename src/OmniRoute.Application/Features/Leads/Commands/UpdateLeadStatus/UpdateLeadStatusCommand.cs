using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Commands.UpdateLeadStatus;

public record UpdateLeadStatusCommand(
    Guid LeadId,
    string NewStatus,
    string? Note,
    string? LostReason,
    string? CancelReason,
    string? WonDetails) : ICommand<UpdateLeadStatusResponse>;
