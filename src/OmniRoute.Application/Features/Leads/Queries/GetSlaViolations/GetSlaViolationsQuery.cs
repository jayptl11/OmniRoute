using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetSlaViolations;

public record GetSlaViolationsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<SlaViolationDto>>;
