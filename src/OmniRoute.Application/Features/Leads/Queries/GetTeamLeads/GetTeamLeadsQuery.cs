using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetTeamLeads;

public record GetTeamLeadsQuery(
    string? Search,
    string? Status,
    string? PriorityLevel,
    string? Channel,
    Guid? AssignedUserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TeamLeadListItemDto>>;
