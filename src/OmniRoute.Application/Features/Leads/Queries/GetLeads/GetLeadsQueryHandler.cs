using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.Leads.Queries.GetLeads;

internal sealed class GetLeadsQueryHandler
    : IQueryHandler<GetLeadsQuery, PagedResult<LeadListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetLeadsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<LeadListItemDto>>> Handle(
        GetLeadsQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var leadsQuery = _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedBy == currentUserId)
            .AsQueryable();

        // TV-07: Tìm kiếm theo SĐT (exact) hoặc tên (contains)
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

        // Lọc theo kênh
        if (RoutingRuleChannelHelper.TryParseChannel(query.Channel, out var channel))
        {
            leadsQuery = leadsQuery.Where(l => l.Channel == channel);
        }

        // Lọc theo ngày tạo
        if (query.DateFrom.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.CreatedAt <= query.DateTo.Value);
        }

        var totalCount = await leadsQuery.CountAsync(ct);

        var leads = await leadsQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new
            {
                l.Id,
                l.LeadCode,
                l.CustomerName,
                l.CustomerPhone,
                l.Channel,
                l.NeedType,
                l.Status,
                l.PriorityLevel,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var items = leads.Select(l => new LeadListItemDto(
            l.Id,
            l.LeadCode,
            l.CustomerName,
            l.CustomerPhone,
            RoutingRuleChannelHelper.GetCanonicalName(l.Channel),
            RoutingRuleChannelHelper.GetDisplayName(l.Channel),
            l.NeedType?.ToString(),
            l.Status.ToString(),
            l.PriorityLevel?.ToString(),
            l.CreatedAt))
            .ToList();

        return Result<PagedResult<LeadListItemDto>>.Success(
            new PagedResult<LeadListItemDto>(items, totalCount, query.Page, query.PageSize));
    }
}
