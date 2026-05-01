using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context) => _context = context;

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<Ticket>> GetByCustomerPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Tickets
            .Where(x => x.CustomerPhone == phone)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<string?> GetMaxTicketCodeTodayAsync(string datePrefix, CancellationToken ct = default)
        => await _context.Tickets
            .Where(x => x.TicketCode.StartsWith(datePrefix))
            .OrderByDescending(x => x.TicketCode)
            .Select(x => x.TicketCode)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
        => await _context.Tickets.AddAsync(ticket, ct);

    public Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _context.Tickets.Update(ticket);
        return Task.CompletedTask;
    }

    public async Task<List<Ticket>> GetActiveTicketsForSlaMonitoringAsync(CancellationToken ct = default)
        => await _context.Tickets
            .Where(x => x.SlaDeadline != null
                        && x.Status != TicketStatus.Resolved
                        && x.Status != TicketStatus.Closed)
            .ToListAsync(ct);

    // CS-01 + CS-03: Danh sách ticket được gán cho nhân viên CS, có filter và phân trang
    public async Task<(List<Ticket> Items, int TotalCount)> GetAssignedTicketsAsync(
        Guid assignedUserId,
        string? search,
        string? status,
        string? priorityLevel,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Tickets
            .Where(x => x.AssignedUserId == assignedUserId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.CustomerPhone == search ||
                x.TicketCode == search ||
                x.CustomerName.Contains(search));

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TicketStatus>(status, ignoreCase: true, out var parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(priorityLevel) &&
            Enum.TryParse<PriorityLevel>(priorityLevel, ignoreCase: true, out var parsedPriority))
            query = query.Where(x => x.PriorityLevel == parsedPriority);

        if (dateFrom.HasValue)
            query = query.Where(x => x.AssignedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.AssignedAt <= dateTo.Value);

        // Sắp xếp: PriorityLevel DESC (High trước), sau đó SlaDeadline ASC (sắp hết hạn trước)
        query = query
            .OrderByDescending(x => x.PriorityLevel)
            .ThenBy(x => x.SlaDeadline);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
