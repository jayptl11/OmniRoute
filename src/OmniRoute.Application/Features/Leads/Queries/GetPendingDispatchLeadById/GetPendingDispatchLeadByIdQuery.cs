using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetPendingDispatchLeadById;

public record GetPendingDispatchLeadByIdQuery(Guid LeadId) : IQuery<PendingDispatchLeadDetailDto>;
