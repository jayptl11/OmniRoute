using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Commands.DispatchLeadToStore;

public record DispatchLeadToStoreCommand(
    Guid LeadId,
    Guid StoreId,
    string? Note
) : ICommand<DispatchLeadToStoreResponse>;
