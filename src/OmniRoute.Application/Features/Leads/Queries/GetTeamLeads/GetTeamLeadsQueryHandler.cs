using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.Queries.GetTeamLeads;

internal sealed class GetTeamLeadsQueryHandler
    : IQueryHandler<GetTeamLeadsQuery, PagedResult<TeamLeadListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetTeamLeadsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<TeamLeadListItemDto>>> Handle(
        GetTeamLeadsQuery query,
        CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;

        if (teamId is null)
            return Result<PagedResult<TeamLeadListItemDto>>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        // Resolve team member IDs to scope lead query (avoids navigation property translation issues)
        var teamMemberIds = await _db.Users
            .Where(u => u.TeamId == teamId)
            .Select(u => u.UserId)
            .ToListAsync(ct);

        var q = _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId.HasValue && teamMemberIds.Contains(l.AssignedUserId.Value))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(l => l.CustomerPhone == search || l.CustomerName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeadStatus>(query.Status, ignoreCase: true, out var status))
            q = q.Where(l => l.Status == status);

        if (!string.IsNullOrWhiteSpace(query.PriorityLevel) &&
            Enum.TryParse<PriorityLevel>(query.PriorityLevel, ignoreCase: true, out var priority))
            q = q.Where(l => l.PriorityLevel == priority);

        if (!string.IsNullOrWhiteSpace(query.Channel) &&
            Enum.TryParse<Channel>(query.Channel, ignoreCase: true, out var channel))
            q = q.Where(l => l.Channel == channel);

        if (query.AssignedUserId.HasValue)
            q = q.Where(l => l.AssignedUserId == query.AssignedUserId.Value);

        if (query.DateFrom.HasValue)
            q = q.Where(l => l.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(l => l.CreatedAt <= query.DateTo.Value);

        var totalCount = await q.CountAsync(ct);

        // Build a lookup for assigned user names
        var userNames = await _db.Users
            .Where(u => teamMemberIds.Contains(u.UserId))
            .Select(u => new { u.UserId, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        var items = await q
            .OrderByDescending(l => l.PriorityLevel)
            .ThenBy(l => l.SlaDeadline)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new
            {
                l.Id,
                l.LeadCode,
                l.CustomerName,
                l.CustomerPhone,
                l.NeedType,
                l.Status,
                l.PriorityLevel,
                l.SlaDeadline,
                l.SlaViolated,
                l.AssignedUserId
            })
            .ToListAsync(ct);

        var dtos = items.Select(l => new TeamLeadListItemDto(
            l.Id,
            l.LeadCode,
            l.CustomerName,
            l.CustomerPhone,
            l.NeedType?.ToString(),
            l.Status.ToString(),
            l.PriorityLevel?.ToString(),
            l.SlaDeadline,
            l.SlaViolated,
            l.AssignedUserId,
            l.AssignedUserId.HasValue && userNames.TryGetValue(l.AssignedUserId.Value, out var name) ? name : null))
            .ToList();

        return Result<PagedResult<TeamLeadListItemDto>>.Success(
            new PagedResult<TeamLeadListItemDto>(dtos, totalCount, query.Page, query.PageSize));
    }
}
