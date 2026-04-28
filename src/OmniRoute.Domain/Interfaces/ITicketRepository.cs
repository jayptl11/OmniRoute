using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<Ticket>> GetByCustomerPhoneAsync(string phone, CancellationToken ct = default);

    Task<string?> GetMaxTicketCodeTodayAsync(string datePrefix, CancellationToken ct = default);

    Task AddAsync(Ticket ticket, CancellationToken ct = default);

    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);

    Task<List<Ticket>> GetActiveTicketsForSlaMonitoringAsync(CancellationToken ct = default);

    /// <summary>
    /// CS-01 + CS-03: Danh sách ticket được gán cho nhân viên CS, có filter và phân trang.
    /// </summary>
    Task<(List<Ticket> Items, int TotalCount)> GetAssignedTicketsAsync(
        Guid assignedUserId,
        string? search,
        string? status,
        string? priorityLevel,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
