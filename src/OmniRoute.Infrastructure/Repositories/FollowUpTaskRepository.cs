using Microsoft.EntityFrameworkCore;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.Repositories;

internal sealed class FollowUpTaskRepository : IFollowUpTaskRepository
{
    private readonly AppDbContext _context;

    public FollowUpTaskRepository(AppDbContext context) => _context = context;

    public async Task<FollowUpTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FollowUpTasks.FindAsync([id], ct);

    public async Task AddAsync(FollowUpTask task, CancellationToken ct = default)
        => await _context.FollowUpTasks.AddAsync(task, ct);

    public async Task<List<FollowUpTask>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.FollowUpTasks
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

    public async Task<List<FollowUpTask>> GetPendingForNotificationAsync(DateTime notifyBefore, CancellationToken ct = default)
        => await _context.FollowUpTasks
            .Where(t => !t.IsCompleted
                && t.NotificationSentAt == null
                && t.DueAt <= notifyBefore)
            .Include(t => t.Lead)
            .ToListAsync(ct);
}
