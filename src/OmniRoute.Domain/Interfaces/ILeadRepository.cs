using OmniRoute.Domain.Entities;

namespace OmniRoute.Domain.Interfaces;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Lead?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<List<Lead>> GetByPhoneAllAsync(string phone, CancellationToken ct = default);
    Task<string?> GetMaxLeadCodeTodayAsync(string datePrefix, CancellationToken ct = default);
    Task AddAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
    Task<List<Lead>> GetActiveLeadsForSlaMonitoringAsync(CancellationToken ct = default);

    // DP-01 + DP-07: Queue danh sách lead chờ điều phối, có filter + phân trang
    Task<(List<Lead> Items, int TotalCount)> GetPendingDispatchLeadsAsync(
        string? search,
        string? priorityLevel,
        string? addressContains,
        int? waitedMoreThanMinutes,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // DP-06: Lịch sử phân công bởi một DP user cụ thể
    Task<List<Lead>> GetDispatchedByUserAsync(Guid dispatchedByUserId, CancellationToken ct = default);
}
