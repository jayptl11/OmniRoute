using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.BackgroundServices;

/// <summary>
/// FOLLOW_UP_DUE — Runs every minute to notify users 30 minutes before a follow-up task is due.
/// Per spec SA-06: system sends notification to user 30 minutes before DueAt.
/// Uses NotificationSentAt flag on FollowUpTask to prevent duplicate notifications.
/// </summary>
public sealed class FollowUpDueMonitoringService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FollowUpDueMonitoringService> _logger;

    public FollowUpDueMonitoringService(
        IServiceScopeFactory scopeFactory,
        ILogger<FollowUpDueMonitoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var followUpTaskRepository = scope.ServiceProvider.GetRequiredService<IFollowUpTaskRepository>();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var notifyBefore = DateTime.UtcNow.AddMinutes(30);
            var tasks = await followUpTaskRepository.GetPendingForNotificationAsync(notifyBefore, ct);

            if (tasks.Count == 0) return;

            foreach (var task in tasks)
            {
                var leadCode = task.Lead?.LeadCode ?? task.LeadId.ToString()[..8];
                var notification = Notification.Create(
                    userId: task.UserId,
                    type: "FOLLOW_UP_DUE",
                    title: $"Nhắc nhở follow-up: {leadCode}",
                    body: $"Bạn có lịch nhắc nhở lúc {task.DueAt:HH:mm dd/MM/yyyy}. Ghi chú: {task.Note}",
                    entityType: "LEAD",
                    entityId: task.LeadId);

                await notificationRepository.AddAsync(notification, ct);
                task.MarkNotificationSent();

                _logger.LogInformation(
                    "FOLLOW_UP_DUE notification sent for task {TaskId}, user {UserId}, due {DueAt}",
                    task.Id, task.UserId, task.DueAt);
            }

            await context.SaveChangesAsync(ct);
            _logger.LogDebug("FollowUpDue cycle: {Count} notification(s) sent.", tasks.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during FollowUpDue monitoring cycle.");
        }
    }
}
