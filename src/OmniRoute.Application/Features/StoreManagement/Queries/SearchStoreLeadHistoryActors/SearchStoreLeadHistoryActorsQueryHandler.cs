using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.DTOs;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Constants;

namespace OmniRoute.Application.Features.StoreManagement.Queries.SearchStoreLeadHistoryActors;

internal sealed class SearchStoreLeadHistoryActorsQueryHandler
    : IQueryHandler<SearchStoreLeadHistoryActorsQuery, List<UserPickerOptionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public SearchStoreLeadHistoryActorsQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<UserPickerOptionDto>>> Handle(
        SearchStoreLeadHistoryActorsQuery query,
        CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
        {
            return Result<List<UserPickerOptionDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");
        }

        var storeLeadIds = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedStoreId == storeId)
            .Select(l => l.Id)
            .ToListAsync(ct);

        if (storeLeadIds.Count == 0)
        {
            return Result<List<UserPickerOptionDto>>.Success([]);
        }

        var actorIds = await _db.ActivityLogs
            .AsNoTracking()
            .Where(al =>
                al.EntityType == "LEAD" &&
                al.PerformedBy.HasValue &&
                storeLeadIds.Contains(al.EntityId))
            .Select(al => al.PerformedBy!.Value)
            .Distinct()
            .ToListAsync(ct);

        if (actorIds.Count == 0)
        {
            return Result<List<UserPickerOptionDto>>.Success([]);
        }

        var search = query.Q?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var normalizedSearch = search?.ToLowerInvariant();

        var usersQuery = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => actorIds.Contains(u.UserId));

        if (hasSearch)
        {
            usersQuery = usersQuery.Where(u =>
                u.Username.ToLower().Contains(normalizedSearch!) ||
                (((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim()).ToLower().Contains(normalizedSearch!) ||
                (((u.LastName ?? string.Empty) + " " + (u.FirstName ?? string.Empty)).Trim()).ToLower().Contains(normalizedSearch!));
        }

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(30)
            .Select(u => new
            {
                u.UserId,
                FullName = ($"{u.FirstName ?? string.Empty} {u.LastName ?? string.Empty}".Trim() != string.Empty
                    ? $"{u.FirstName ?? string.Empty} {u.LastName ?? string.Empty}".Trim()
                    : u.Username),
                RoleName = u.Role != null ? u.Role.RoleName : null
            })
            .ToListAsync(ct);

        return Result<List<UserPickerOptionDto>>.Success(
            users.Select(u => new UserPickerOptionDto(
                u.UserId,
                u.FullName,
                u.RoleName,
                RoleCatalog.GetDisplayName(u.RoleName)))
            .ToList());
    }
}
