using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetFollowUpTasks;

/// <param name="Filter">null = tất cả | "today" = hôm nay | "upcoming" = sắp đến | "overdue" = đã quá hạn</param>
public record GetFollowUpTasksQuery(string? Filter) : IQuery<List<FollowUpTaskListItemDto>>;
