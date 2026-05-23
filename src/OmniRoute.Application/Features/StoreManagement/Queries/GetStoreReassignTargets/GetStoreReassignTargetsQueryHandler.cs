using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.DTOs;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreReassignTargets;

internal sealed class GetStoreReassignTargetsQueryHandler
    : IQueryHandler<GetStoreReassignTargetsQuery, List<UserPickerOptionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetStoreReassignTargetsQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<UserPickerOptionDto>>> Handle(
        GetStoreReassignTargetsQuery query,
        CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
        {
            return Result<List<UserPickerOptionDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");
        }

        var lead = await _db.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == query.LeadId, ct);

        if (lead is null)
        {
            return Result<List<UserPickerOptionDto>>.Failure("LEAD_NOT_FOUND", "Không tìm thấy lead.");
        }

        if (lead.AssignedStoreId != storeId)
        {
            return Result<List<UserPickerOptionDto>>.Failure("LEAD_NOT_IN_STORE", "Lead này không thuộc đơn vị của bạn.");
        }

        if (lead.Status is LeadStatus.Won or LeadStatus.Lost or LeadStatus.Cancelled)
        {
            return Result<List<UserPickerOptionDto>>.Failure("LEAD_TERMINAL", "Không thể reassign lead đã đóng (Won/Lost/Cancelled).");
        }

        var search = query.Q?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var normalizedSearch = search?.ToLowerInvariant();

        var usersQuery = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u =>
                u.StoreId == storeId &&
                u.IsActive &&
                u.UserId != lead.AssignedUserId);

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
