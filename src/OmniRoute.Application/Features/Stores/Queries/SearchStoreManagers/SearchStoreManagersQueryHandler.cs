using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Stores.DTOs;

namespace OmniRoute.Application.Features.Stores.Queries.SearchStoreManagers;

internal sealed class SearchStoreManagersQueryHandler
    : IQueryHandler<SearchStoreManagersQuery, List<StoreManagerDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchStoreManagersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<StoreManagerDto>>> Handle(
        SearchStoreManagersQuery query,
        CancellationToken ct)
    {
        var q = _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role != null && u.Role.RoleName == "QL");

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLower();
            q = q.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)));
        }

        var users = await q
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(30)
            .Select(u => new
            {
                u.UserId,
                u.Username,
                u.FirstName,
                u.LastName,
                u.StoreId
            })
            .ToListAsync(ct);

        // Load store names for users that already have a store
        var storeIds = users
            .Where(u => u.StoreId.HasValue)
            .Select(u => u.StoreId!.Value)
            .Distinct()
            .ToList();

        var storeNames = storeIds.Count > 0
            ? await _db.Stores
                .Where(s => storeIds.Contains(s.Id))
                .Select(s => new { s.Id, s.StoreName })
                .ToDictionaryAsync(s => s.Id, s => s.StoreName, ct)
            : new Dictionary<Guid, string>();

        var dtos = users.Select(u => new StoreManagerDto(
            u.UserId,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.Username,
            u.StoreId.HasValue,
            u.StoreId.HasValue ? storeNames.GetValueOrDefault(u.StoreId.Value) : null))
            .ToList();

        return Result<List<StoreManagerDto>>.Success(dtos);
    }
}
