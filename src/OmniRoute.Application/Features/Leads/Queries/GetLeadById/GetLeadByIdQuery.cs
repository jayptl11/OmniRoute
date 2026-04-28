using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetLeadById;

public record GetLeadByIdQuery(Guid LeadId) : IQuery<LeadDetailDto>;
