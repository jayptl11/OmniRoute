using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetFollowUpTasks;

internal sealed class GetFollowUpTasksQueryHandler
    : IQueryHandler<GetFollowUpTasksQuery, List<FollowUpTaskListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetFollowUpTasksQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<FollowUpTaskListItemDto>>> Handle(
        GetFollowUpTasksQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        var q = _db.FollowUpTasks
            .AsNoTracking()
            .Include(t => t.Lead)
            .Where(t => t.UserId == currentUserId && !t.IsCompleted)
            .AsQueryable();

        // SA-07: Lọc theo nhóm thời gian
        q = query.Filter?.ToLowerInvariant() switch
        {
            "today"    => q.Where(t => t.DueAt >= todayStart && t.DueAt < todayEnd),
            "upcoming" => q.Where(t => t.DueAt >= now),
            "overdue"  => q.Where(t => t.DueAt < now),
            _          => q   // null = tất cả
        };

        var items = await q
            .OrderBy(t => t.DueAt)
            .Select(t => new FollowUpTaskListItemDto(
                t.Id,
                t.LeadId,
                t.Lead != null ? t.Lead.LeadCode : string.Empty,
                t.Lead != null ? t.Lead.CustomerName : string.Empty,
                t.Lead != null ? t.Lead.CustomerPhone : string.Empty,
                t.DueAt,
                t.Note,
                t.DueAt < now,
                t.DueAt >= todayStart && t.DueAt < todayEnd))
            .ToListAsync(ct);

        return Result<List<FollowUpTaskListItemDto>>.Success(items);
    }
}
