using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.Leads.Queries.GetAssignedLeads;

internal sealed class GetAssignedLeadsQueryHandler
    : IQueryHandler<GetAssignedLeadsQuery, PagedResult<SaleLeadListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetAssignedLeadsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<SaleLeadListItemDto>>> Handle(
        GetAssignedLeadsQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var leadsQuery = _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId == currentUserId)
            .AsQueryable();

        // SA-03: Tìm kiếm theo SĐT (exact) hoặc tên (contains)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            leadsQuery = leadsQuery.Where(l => l.CustomerPhone == search || l.CustomerName.Contains(search));
        }

        // Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeadStatus>(query.Status, ignoreCase: true, out var status))
        {
            leadsQuery = leadsQuery.Where(l => l.Status == status);
        }

        // Lọc theo mức ưu tiên
        if (!string.IsNullOrWhiteSpace(query.PriorityLevel) &&
            Enum.TryParse<PriorityLevel>(query.PriorityLevel, ignoreCase: true, out var priority))
        {
            leadsQuery = leadsQuery.Where(l => l.PriorityLevel == priority);
        }

        // Lọc theo kênh
        if (RoutingRuleChannelHelper.TryParseChannel(query.Channel, out var channel))
        {
            leadsQuery = leadsQuery.Where(l => l.Channel == channel);
        }

        // Lọc theo ngày được gán
        if (query.DateFrom.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.AssignedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.AssignedAt <= query.DateTo.Value);
        }

        var totalCount = await leadsQuery.CountAsync(ct);

        // SA-01: Sắp xếp priority DESC (High -> Medium -> Low), sau đó SlaDeadline ASC.
        var leads = await leadsQuery
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
                l.AssignedAt
            })
            .ToListAsync(ct);

        var items = leads
            .Select(l => new SaleLeadListItemDto(
                l.Id,
                l.LeadCode,
                l.CustomerName,
                l.CustomerPhone,
                l.NeedType?.ToString(),
                l.Status.ToString(),
                l.PriorityLevel?.ToString(),
                l.SlaDeadline,
                l.SlaViolated,
                l.AssignedAt))
            .ToList();

        return Result<PagedResult<SaleLeadListItemDto>>.Success(
            new PagedResult<SaleLeadListItemDto>(items, totalCount, query.Page, query.PageSize));
    }
}
