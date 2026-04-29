using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreLeadHistory;

public record GetStoreLeadHistoryQuery(
    Guid? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<StoreLeadHistoryItemDto>>;
