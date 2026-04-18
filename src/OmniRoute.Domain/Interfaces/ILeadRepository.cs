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
}
