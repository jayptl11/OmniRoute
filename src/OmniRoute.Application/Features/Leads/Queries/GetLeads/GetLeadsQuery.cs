using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetLeads;

public record GetLeadsQuery(
    string? Search,
    string? Status,
    string? Channel,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<LeadListItemDto>>;
