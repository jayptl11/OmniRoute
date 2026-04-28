using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetPendingDispatchLeads;

public record GetPendingDispatchLeadsQuery(
    string? Search,
    string? PriorityLevel,
    string? AddressContains,
    int? WaitedMoreThanMinutes,
    int Page = 1,
    int PageSize = 20
) : IQuery<GetPendingDispatchLeadsResult>;

public record GetPendingDispatchLeadsResult(
    List<PendingDispatchLeadListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
