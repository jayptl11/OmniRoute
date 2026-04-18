using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
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
}
