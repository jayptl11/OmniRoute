using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetDispatchHistory;

public record GetDispatchHistoryQuery : IQuery<List<DispatchHistoryItemDto>>;
