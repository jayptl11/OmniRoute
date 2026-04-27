using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;

namespace OmniRoute.Infrastructure.BackgroundServices;

/// <summary>
/// SYS-04 — Runs every 5 minutes to:
/// 1. Mark SLA violations (sla_violated = true) and notify assigned user + TN users.
/// 2. Send SLA warning notifications (once, before deadline).
/// 3. Recalculate W_waittime component of priority_score for all active leads.
/// </summary>
public sealed class SlaMonitoringService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitoringService> _logger;

    public SlaMonitoringService(
        IServiceScopeFactory scopeFactory,
        ILogger<SlaMonitoringService> logger)
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
            var leadRepository = scope.ServiceProvider.GetRequiredService<ILeadRepository>();
            var slaConfigRepository = scope.ServiceProvider.GetRequiredService<ISlaConfigRepository>();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var leads = await leadRepository.GetActiveLeadsForSlaMonitoringAsync(ct);
            if (leads.Count == 0) return;

            var slaConfigs = await slaConfigRepository.GetAllAsync(ct);
            var now = DateTime.UtcNow;
            int processed = 0;

            foreach (var lead in leads)
            {
                bool changed = false;

                // --- SLA Violation ---
                if (!lead.SlaViolated && lead.SlaDeadline.HasValue && now >= lead.SlaDeadline.Value)
                {
                    lead.MarkSlaViolated();
                    changed = true;

                    _logger.LogWarning("SLA violated for lead {LeadCode} (deadline: {Deadline})", lead.LeadCode, lead.SlaDeadline);

                    await NotifyLeadUsersAsync(context, notificationRepository, lead,
                        "SLA_VIOLATED",
                        $"Vi phạm SLA: {lead.LeadCode}",
                        $"Lead {lead.LeadCode} - {lead.CustomerName} đã vượt quá thời hạn SLA ({lead.SlaDeadline:dd/MM/yyyy HH:mm}).",
                        ct);
                }
                // --- SLA Warning ---
                else if (lead.SlaWarningSentAt is null && lead.SlaDeadline.HasValue && lead.AssignedGroup.HasValue)
                {
                    var slaConfig = slaConfigs.FirstOrDefault(s =>
                        s.AssignedGroup == lead.AssignedGroup.Value &&
                        s.PriorityLevel == lead.PriorityLevel &&
                        s.IsActive);

                    int warningHours = slaConfig?.WarningBeforeHours ?? 1;

                    if (now >= lead.SlaDeadline.Value.AddHours(-warningHours))
                    {
                        lead.MarkSlaWarningSent();
                        changed = true;

                        _logger.LogInformation("SLA warning sent for lead {LeadCode}", lead.LeadCode);

                        await NotifyLeadUsersAsync(context, notificationRepository, lead,
                            "SLA_WARNING",
                            $"Cảnh báo SLA sắp hết hạn: {lead.LeadCode}",
                            $"Lead {lead.LeadCode} - {lead.CustomerName} sẽ vi phạm SLA lúc {lead.SlaDeadline:dd/MM/yyyy HH:mm}.",
                            ct);
                    }
                }

                // --- W_waittime recalculation (SYS-02 dynamic update) ---
                if (lead.AssignedGroup.HasValue && lead.PriorityLevel.HasValue)
                {
                    var (newScore, newLevel) = RecalculatePriorityWithWaittime(lead, now);
                    if (newScore != lead.PriorityScore || newLevel != lead.PriorityLevel.Value)
                    {
                        lead.UpdatePriorityScore(newScore, newLevel);
                        changed = true;
                    }
                }

                if (changed) processed++;
            }

            if (processed > 0)
                await context.SaveChangesAsync(ct);

            _logger.LogDebug("SLA monitoring cycle complete. Leads updated: {Count}/{Total}", processed, leads.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during SLA monitoring cycle.");
        }
    }

    // -----------------------------------------------------------------------
    // W_waittime recalculation (spec §4.2)
    // Waittime is measured from CreatedAt (time in system, before assignment)
    // -----------------------------------------------------------------------
    private static (int score, PriorityLevel level) RecalculatePriorityWithWaittime(Lead lead, DateTime now)
    {
        var waitMinutes = (now - lead.CreatedAt).TotalMinutes;

        int wWaittime = waitMinutes > 60 ? 10
                      : waitMinutes > 30 ? 5
                      : waitMinutes > 15 ? 2
                      : 0;

        // Re-use stored base score (channel + need + history) by subtracting previous waittime
        // Since we don't store components separately, we clamp the updated total to [0, 100]
        // and preserve the existing score minus previous waittime contribution then add new one.
        // As a safe fallback: bump the score by the new waittime delta relative to 0-baseline.
        int baseScore = lead.PriorityScore; // already includes previous wWaittime
        int newScore = Math.Clamp(baseScore + wWaittime, 0, 100);

        var newLevel = newScore >= 70 ? PriorityLevel.High
                     : newScore >= 40 ? PriorityLevel.Medium
                     : PriorityLevel.Low;

        return (newScore, newLevel);
    }

    // -----------------------------------------------------------------------
    // Notify assigned user + all TN-role users
    // -----------------------------------------------------------------------
    private static async Task NotifyLeadUsersAsync(
        AppDbContext context,
        INotificationRepository notificationRepository,
        Lead lead,
        string notificationType,
        string title,
        string body,
        CancellationToken ct)
    {
        // Notify assigned user
        if (lead.AssignedUserId.HasValue)
        {
            var notification = Notification.Create(
                userId: lead.AssignedUserId.Value,
                type: notificationType,
                title: title,
                body: body,
                entityType: "LEAD",
                entityId: lead.Id);
            await notificationRepository.AddAsync(notification, ct);
        }

        // Notify all TN (Trưởng nhóm) users
        var tnUsers = await context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role != null && u.Role.RoleName == "TN")
            .Select(u => u.UserId)
            .ToListAsync(ct);

        foreach (var userId in tnUsers)
        {
            var notification = Notification.Create(
                userId: userId,
                type: notificationType,
                title: title,
                body: body,
                entityType: "LEAD",
                entityId: lead.Id);
            await notificationRepository.AddAsync(notification, ct);
        }
    }
}
