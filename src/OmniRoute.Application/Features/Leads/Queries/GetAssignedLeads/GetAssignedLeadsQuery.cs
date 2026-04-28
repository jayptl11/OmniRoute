using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetAssignedLeads;

public record GetAssignedLeadsQuery(
    string? Search,
    string? Status,
    string? PriorityLevel,
    string? Channel,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<SaleLeadListItemDto>>;
