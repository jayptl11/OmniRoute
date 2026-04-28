using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetAssignedLeadById;

public record GetAssignedLeadByIdQuery(Guid LeadId) : IQuery<SaleLeadDetailDto>;
