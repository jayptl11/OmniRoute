using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Commands.UpdateLead;

public record UpdateLeadCommand(
    Guid LeadId,
    string? CustomerAddress,
    string? CustomerEmail,
    List<string>? ProductInterest,
    string? NeedDescription) : ICommand<UpdateLeadResponse>;
