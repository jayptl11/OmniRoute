using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreLeads;

internal sealed class GetStoreLeadsQueryHandler
    : IQueryHandler<GetStoreLeadsQuery, PagedResult<StoreLeadListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetStoreLeadsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<StoreLeadListItemDto>>> Handle(
        GetStoreLeadsQuery query,
        CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
        {
            return Result<PagedResult<StoreLeadListItemDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");
        }

        var leadsQuery = _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedStoreId == storeId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            leadsQuery = leadsQuery.Where(l => l.CustomerPhone == search || l.CustomerName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeadStatus>(query.Status, ignoreCase: true, out var status))
        {
            leadsQuery = leadsQuery.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.PriorityLevel) &&
            Enum.TryParse<PriorityLevel>(query.PriorityLevel, ignoreCase: true, out var priority))
        {
            leadsQuery = leadsQuery.Where(l => l.PriorityLevel == priority);
        }

        if (RoutingRuleChannelHelper.TryParseChannel(query.Channel, out var channel))
        {
            leadsQuery = leadsQuery.Where(l => l.Channel == channel);
        }

        if (query.AssignedUserId.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.AssignedUserId == query.AssignedUserId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.CreatedAt <= query.DateTo.Value);
        }

        var totalCount = await leadsQuery.CountAsync(ct);

        var staffIds = await _db.Users
            .Where(u => u.StoreId == storeId)
            .Select(u => u.UserId)
            .ToListAsync(ct);

        var userNames = await _db.Users
            .Where(u => staffIds.Contains(u.UserId))
            .Select(u => new { u.UserId, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        var items = await leadsQuery
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

        var dtos = items.Select(l => new StoreLeadListItemDto(
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

        return Result<PagedResult<StoreLeadListItemDto>>.Success(
            new PagedResult<StoreLeadListItemDto>(dtos, totalCount, query.Page, query.PageSize));
    }
}
