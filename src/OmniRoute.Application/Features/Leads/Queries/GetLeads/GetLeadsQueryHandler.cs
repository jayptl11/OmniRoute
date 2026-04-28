using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;

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

        var q = _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedBy == currentUserId)
            .AsQueryable();

        // TV-07: Tìm kiếm theo SĐT (exact) hoặc tên (contains)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(l => l.CustomerPhone == search || l.CustomerName.Contains(search));
        }

        // Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeadStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(l => l.Status == status);
        }

        // Lọc theo kênh
        if (!string.IsNullOrWhiteSpace(query.Channel) &&
            Enum.TryParse<Channel>(query.Channel, ignoreCase: true, out var channel))
        {
            q = q.Where(l => l.Channel == channel);
        }

        // Lọc theo ngày tạo
        if (query.DateFrom.HasValue)
            q = q.Where(l => l.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(l => l.CreatedAt <= query.DateTo.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new LeadListItemDto(
                l.Id,
                l.LeadCode,
                l.CustomerName,
                l.CustomerPhone,
                l.Channel.ToString(),
                l.NeedType != null ? l.NeedType.ToString() : null,
                l.Status.ToString(),
                l.PriorityLevel != null ? l.PriorityLevel.ToString() : null,
                l.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<LeadListItemDto>>.Success(
            new PagedResult<LeadListItemDto>(items, totalCount, query.Page, query.PageSize));
    }
}
