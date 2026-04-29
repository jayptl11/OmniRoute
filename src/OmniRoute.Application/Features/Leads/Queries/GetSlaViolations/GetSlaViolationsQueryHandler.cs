using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.Queries.GetSlaViolations;

internal sealed class GetSlaViolationsQueryHandler
    : IQueryHandler<GetSlaViolationsQuery, PagedResult<SlaViolationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetSlaViolationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<SlaViolationDto>>> Handle(
        GetSlaViolationsQuery query,
        CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;

        if (teamId is null)
            return Result<PagedResult<SlaViolationDto>>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        // Use max WarningBeforeHours across all active SLA configs as the near-deadline window
        var maxWarningHours = await _db.SlaConfigs
            .Where(s => s.IsActive)
            .MaxAsync(s => (int?)s.WarningBeforeHours, ct) ?? 4;

        var warningThreshold = DateTime.UtcNow.AddHours(maxWarningHours);

        var teamMemberIds = await _db.Users
            .Where(u => u.TeamId == teamId)
            .Select(u => u.UserId)
            .ToListAsync(ct);

        var terminalStatuses = new[] { LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };

        var q = _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId.HasValue && teamMemberIds.Contains(l.AssignedUserId.Value))
            .Where(l => !terminalStatuses.Contains(l.Status))
            .Where(l => l.SlaDeadline != null)
            .Where(l => l.SlaViolated || l.SlaDeadline <= warningThreshold);

        var totalCount = await q.CountAsync(ct);

        var now = DateTime.UtcNow;

        var userNames = await _db.Users
            .Where(u => teamMemberIds.Contains(u.UserId))
            .Select(u => new { u.UserId, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        var items = await q
            .OrderByDescending(l => l.SlaViolated)
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

        var dtos = items.Select(l => new SlaViolationDto(
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
            l.AssignedUserId.HasValue && userNames.TryGetValue(l.AssignedUserId.Value, out var name) ? name : null,
            l.SlaDeadline.HasValue ? Math.Round((l.SlaDeadline.Value - now).TotalHours, 1) : null))
            .ToList();

        return Result<PagedResult<SlaViolationDto>>.Success(
            new PagedResult<SlaViolationDto>(dtos, totalCount, query.Page, query.PageSize));
    }
}
