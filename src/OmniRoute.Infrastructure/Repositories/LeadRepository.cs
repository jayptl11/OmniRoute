using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class LeadRepository : ILeadRepository
{
    private readonly AppDbContext _context;

    public LeadRepository(AppDbContext context) => _context = context;

    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Lead?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Leads
            .Where(x => x.CustomerPhone == phone)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Lead>> GetByPhoneAllAsync(string phone, CancellationToken ct = default)
        => await _context.Leads
            .Where(x => x.CustomerPhone == phone)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<string?> GetMaxLeadCodeTodayAsync(string prefix, CancellationToken ct = default)
        => await _context.Leads
            .Where(x => x.LeadCode.StartsWith(prefix))
            .OrderByDescending(x => x.LeadCode)
            .Select(x => x.LeadCode)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Lead lead, CancellationToken ct = default)
        => await _context.Leads.AddAsync(lead, ct);

    public Task UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        _context.Leads.Update(lead);
        return Task.CompletedTask;
    }

    public async Task<List<Lead>> GetActiveLeadsForSlaMonitoringAsync(CancellationToken ct = default)
        => await _context.Leads
            .Where(x => x.SlaDeadline != null
                        && x.Status != LeadStatus.Won
                        && x.Status != LeadStatus.Lost
                        && x.Status != LeadStatus.Cancelled)
            .ToListAsync(ct);

    // DP-01 + DP-07: Queue lead chờ điều phối, có filter + phân trang
    public async Task<(List<Lead> Items, int TotalCount)> GetPendingDispatchLeadsAsync(
        string? search,
        string? priorityLevel,
        string? addressContains,
        int? waitedMoreThanMinutes,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Leads
            .Where(x => x.Status == LeadStatus.PendingDispatch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.CustomerPhone == search ||
                x.CustomerName.Contains(search));

        if (!string.IsNullOrWhiteSpace(priorityLevel) &&
            Enum.TryParse<PriorityLevel>(priorityLevel, ignoreCase: true, out var level))
            query = query.Where(x => x.PriorityLevel == level);

        if (!string.IsNullOrWhiteSpace(addressContains))
            query = query.Where(x => x.CustomerAddress != null && x.CustomerAddress.Contains(addressContains));

        if (waitedMoreThanMinutes.HasValue)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-waitedMoreThanMinutes.Value);
            query = query.Where(x => x.CreatedAt <= threshold);
        }

        // Sắp xếp: HIGH priority trước, sau đó chờ lâu nhất lên đầu (CreatedAt tăng dần)
        query = query
            .OrderByDescending(x => x.PriorityLevel)
            .ThenBy(x => x.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    // DP-06: Lịch sử lead đã được user này phân công (có log DISPATCHED_TO_STORE)
    public async Task<List<Lead>> GetDispatchedByUserAsync(Guid dispatchedByUserId, CancellationToken ct = default)
        => await _context.ActivityLogs
            .Where(al => al.Action == "DISPATCHED_TO_STORE"
                         && al.EntityType == "LEAD"
                         && al.PerformedBy == dispatchedByUserId)
            .Join(_context.Leads,
                al => al.EntityId,
                l => l.Id,
                (al, l) => l)
            .Distinct()
            .ToListAsync(ct);
}
