using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreLeads;

public record GetStoreLeadsQuery(
    string? Search,
    string? Status,
    string? PriorityLevel,
    string? Channel,
    Guid? AssignedUserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<StoreLeadListItemDto>>;
