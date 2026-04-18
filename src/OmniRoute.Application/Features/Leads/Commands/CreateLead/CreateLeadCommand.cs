using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.Commands.CreateLead;

public record CreateLeadCommand(
    string CustomerName,
    string CustomerPhone,
    Channel Channel,
    string NeedDescription,
    string? CustomerAddress,
    string? CustomerEmail,
    List<string>? ProductInterest,
    bool ForceCreate
) : ICommand<CreateLeadResponse>;
