using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Persistence;
using OmniRoute.Infrastructure.Settings;

namespace OmniRoute.Infrastructure.Services;

/// <summary>
/// Implements SYS-01 (Need Classification), SYS-02 (Priority Scoring), SYS-03 (Routing Decision).
/// Executed inline within the create-lead request.
/// </summary>
internal sealed class RoutingEngine : IRoutingEngine
{
    private readonly AppDbContext _context;
    private readonly ILeadRepository _leadRepository;
    private readonly IRoutingRuleRepository _routingRuleRepository;
    private readonly ISlaConfigRepository _slaConfigRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAiClassificationService _aiClassification;
    private readonly IOptions<AiSettings> _aiSettings;
    private readonly ILogger<RoutingEngine> _logger;

    public RoutingEngine(
        AppDbContext context,
        ILeadRepository leadRepository,
        IRoutingRuleRepository routingRuleRepository,
        ISlaConfigRepository slaConfigRepository,
        IActivityLogRepository activityLogRepository,
        INotificationRepository notificationRepository,
        IAiClassificationService aiClassification,
        IOptions<AiSettings> aiSettings,
        ILogger<RoutingEngine> logger)
    {
        _context = context;
        _leadRepository = leadRepository;
        _routingRuleRepository = routingRuleRepository;
        _slaConfigRepository = slaConfigRepository;
        _activityLogRepository = activityLogRepository;
        _notificationRepository = notificationRepository;
        _aiClassification = aiClassification;
        _aiSettings = aiSettings;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid leadId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, ct);
        if (lead is null) return;

        // SYS-01: Need Classification
        var (actionGroup, needType) = await ClassifyNeedAsync(lead, ct);

        // SYS-02: Priority Scoring
        var (priorityScore, priorityLevel) = await CalculatePriorityAsync(lead, ct);

        lead.SetClassification(needType, priorityScore, priorityLevel, actionGroup);
        await _context.SaveChangesAsync(ct);

        // SYS-03: Routing Decision
        await RouteLeadAsync(lead, actionGroup, priorityLevel, ct);
    }

    // -----------------------------------------------------------------------
    // SYS-01: Need Classification
    // -----------------------------------------------------------------------
    private async Task<(AssignedGroup group, NeedType needType)> ClassifyNeedAsync(Lead lead, CancellationToken ct)
    {
        var rules = await _routingRuleRepository.GetActiveRulesOrderedAsync(ct);

        foreach (var rule in rules)
        {
            if (!RuleMatchesChannel(rule, lead.Channel)) continue;
            if (!RuleMatchesKeywords(rule, lead.NeedDescription)) continue;

            // Layer 1 rule matched — no need for AI
            var needType = MapGroupToDefaultNeedType(rule.ActionGroup);
            return (rule.ActionGroup, needType);
        }

        // Layer 1 found no match — try Layer 2 (AI classification)
        if (!string.IsNullOrWhiteSpace(lead.NeedDescription))
        {
            try
            {
                var aiResult = await _aiClassification.ClassifyAsync(
                    lead.NeedDescription,
                    lead.Channel.ToString(),
                    ct);

                if (aiResult is not null && aiResult.ConfidenceScore >= _aiSettings.Value.ConfidenceThreshold)
                {
                    _logger.LogInformation(
                        "AI classified lead {LeadId} as {NeedType} (confidence {Score:F2}, provider {Provider})",
                        lead.Id, aiResult.NeedType, aiResult.ConfidenceScore, aiResult.UsedProvider);

                    var aiGroup = MapNeedTypeToGroup(aiResult.NeedType);
                    return (aiGroup, aiResult.NeedType);
                }

                _logger.LogInformation(
                    "AI classification confidence {Score:F2} below threshold {Threshold:F2} for lead {LeadId}. Falling back to DP.",
                    aiResult?.ConfidenceScore ?? 0, _aiSettings.Value.ConfidenceThreshold, lead.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI classification failed for lead {LeadId}. Falling back.", lead.Id);
            }
        }

        // No rule matched and AI insufficient → send to DP dispatch queue
        return (AssignedGroup.StoreSupport, NeedType.Other);
    }

    private static AssignedGroup MapNeedTypeToGroup(NeedType needType) => needType switch
    {
        NeedType.SaleNew or NeedType.SaleUpgrade or NeedType.SaleRenew => AssignedGroup.Sale,
        NeedType.CskhSupport or NeedType.CskhComplaint or NeedType.CskhWarranty => AssignedGroup.Cskh,
        NeedType.StoreVisit => AssignedGroup.StoreSupport,
        _ => AssignedGroup.StoreSupport  // fallback to DP
    };

    private static bool RuleMatchesChannel(RoutingRule rule, Channel channel)
    {
        if (rule.ConditionChannelJson is null) return true; // null = all channels

        var channels = JsonSerializer.Deserialize<string[]>(rule.ConditionChannelJson);
        if (channels is null || channels.Length == 0) return true;

        return channels.Any(c => string.Equals(c, channel.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool RuleMatchesKeywords(RoutingRule rule, string needDescription)
    {
        if (rule.ConditionKeywordsJson is null) return true; // null = no keyword filter

        var keywords = JsonSerializer.Deserialize<string[]>(rule.ConditionKeywordsJson);
        if (keywords is null || keywords.Length == 0) return true;

        return keywords.Any(kw => needDescription.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    private static NeedType MapGroupToDefaultNeedType(AssignedGroup group) => group switch
    {
        AssignedGroup.Sale => NeedType.SaleNew,
        AssignedGroup.Cskh => NeedType.CskhSupport,
        AssignedGroup.StoreSupport => NeedType.StoreVisit,
        _ => NeedType.Other
    };

    // -----------------------------------------------------------------------
    // SYS-02: Priority Scoring
    // -----------------------------------------------------------------------
    private async Task<(int score, PriorityLevel level)> CalculatePriorityAsync(Lead lead, CancellationToken ct)
    {
        int wChannel = GetChannelScore(lead.Channel);
        int wNeed = GetNeedScore(lead.NeedType);
        int wHistory = await GetHistoryScoreAsync(lead.CustomerPhone, lead.Id, ct);
        int wWaittime = 0; // Always 0 at creation time; recalculated by SYS-04

        int score = Math.Min(100, wChannel + wNeed + wHistory + wWaittime);

        var level = score >= 70 ? PriorityLevel.High
                  : score >= 40 ? PriorityLevel.Medium
                  : PriorityLevel.Low;

        return (score, level);
    }

    private static int GetChannelScore(Channel channel) => channel switch
    {
        Channel.Walkin => 30,
        Channel.Hotline => 25,
        Channel.Chat => 20,
        Channel.Referral => 20,
        Channel.Webform => 15,
        Channel.Email => 10,
        Channel.Zalo => 10,
        _ => 0
    };

    private static int GetNeedScore(NeedType? needType) => needType switch
    {
        NeedType.CskhComplaint => 30,
        NeedType.CskhWarranty => 25,
        NeedType.SaleNew => 20,
        NeedType.SaleUpgrade => 20,
        NeedType.CskhSupport => 15,
        NeedType.SaleRenew => 15,
        NeedType.StoreVisit => 10,
        NeedType.Other => 5,
        null => 0,
        _ => 0
    };

    private async Task<int> GetHistoryScoreAsync(string phone, Guid currentLeadId, CancellationToken ct)
    {
        var previousLeads = await _context.Leads
            .Where(x => x.CustomerPhone == phone && x.Id != currentLeadId)
            .ToListAsync(ct);

        if (!previousLeads.Any()) return 0;

        bool hasWon = previousLeads.Any(x => x.Status == LeadStatus.Won);
        return hasWon ? 15 : 5;
    }

    // -----------------------------------------------------------------------
    // SYS-03: Routing Decision
    // -----------------------------------------------------------------------
    private async Task RouteLeadAsync(Lead lead, AssignedGroup actionGroup, PriorityLevel priorityLevel, CancellationToken ct)
    {
        if (actionGroup == AssignedGroup.StoreSupport)
        {
            lead.SetPendingDispatch();
            await _context.SaveChangesAsync(ct);

            // Notify all DP-role users
            await NotifyRoleUsersAsync(lead, "DP", "PENDING_DISPATCH",
                $"Lead mới cần điều phối: {lead.LeadCode}",
                $"Khách hàng {lead.CustomerName} ({lead.CustomerPhone}) cần hỗ trợ tại cửa hàng.",
                ct);

            await _context.SaveChangesAsync(ct);
            return;
        }

        // Find user with lowest workload in matching team
        var roleName = actionGroup == AssignedGroup.Sale ? "SA" : "CS";
        var assignedUser = await FindLeastLoadedUserAsync(roleName, lead.AssignedStoreId, ct);

        if (assignedUser is null)
        {
            lead.SetPendingAssignment();
            await _context.SaveChangesAsync(ct);

            // Notify team leaders
            await NotifyRoleUsersAsync(lead, "TN", "SYSTEM_ALERT",
                $"Lead {lead.LeadCode} không tìm được nhân viên",
                $"Không tìm được nhân viên {roleName} phù hợp. Vui lòng gán thủ công.",
                ct);

            await _context.SaveChangesAsync(ct);
            return;
        }

        // Calculate SLA deadline
        var slaConfig = await _slaConfigRepository.GetByGroupAndPriorityAsync(actionGroup, priorityLevel, ct);
        int maxHours = slaConfig?.MaxHours ?? 8;
        var slaDeadline = DateTime.UtcNow.AddHours(maxHours);

        lead.AssignToUser(assignedUser.UserId, slaDeadline);
        assignedUser.IncrementWorkload();
        assignedUser.UpdateLastAssigned();

        await _context.SaveChangesAsync(ct);

        // Notify assigned user
        var notification = Notification.Create(
            userId: assignedUser.UserId,
            type: "NEW_LEAD",
            title: $"Lead mới được gán: {lead.LeadCode}",
            body: $"Khách hàng {lead.CustomerName} ({lead.CustomerPhone}) - {lead.NeedDescription[..Math.Min(100, lead.NeedDescription.Length)]}",
            entityType: "LEAD",
            entityId: lead.Id
        );
        await _notificationRepository.AddAsync(notification, ct);

        // Log routing completion
        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "ROUTING_COMPLETED",
            performedBy: null,
            newValue: System.Text.Json.JsonSerializer.Serialize(new
            {
                AssignedGroup = actionGroup.ToString(),
                AssignedUserId = assignedUser.UserId,
                PriorityLevel = priorityLevel.ToString(),
                SlaDeadline = slaDeadline
            })
        );
        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        // CS group: create Ticket entity so CS-01..CS-08 endpoints can surface it
        if (actionGroup == AssignedGroup.Cskh)
        {
            var ticket = await BuildTicketFromLeadAsync(lead, assignedUser.UserId, slaDeadline, ct);
            await _context.Tickets.AddAsync(ticket, ct);
            await _context.SaveChangesAsync(ct);
        }
    }

    // DP-04b: Sau khi dispatch về store, tự gán SS ít việc nhất trong store đó (BR-02)
    public async Task AssignToStoreStaffAsync(Guid leadId, Guid storeId, CancellationToken ct = default)
    {
        const int maxLeadsPerPerson = 20;

        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
        if (lead is null) return;

        var candidate = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive
                        && u.Role != null
                        && u.Role.RoleName == "SS"
                        && u.StoreId == storeId
                        && u.CurrentWorkload < maxLeadsPerPerson)
            .OrderBy(u => u.CurrentWorkload)
            .ThenBy(u => u.LastAssignedAt ?? DateTime.MinValue)
            .FirstOrDefaultAsync(ct);

        if (candidate is null) return; // Không có SS available — QL xử lý thủ công

        lead.AssignUserAfterDispatch(candidate.UserId);
        candidate.IncrementWorkload();

        var notification = Notification.Create(
            userId: candidate.UserId,
            type: "LEAD_ASSIGNED",
            title: $"Lead mới được phân công: {lead.LeadCode}",
            body: $"Khách hàng {lead.CustomerName} ({lead.CustomerPhone}) đã được phân công cho bạn.",
            entityType: "LEAD",
            entityId: lead.Id);
        await _notificationRepository.AddAsync(notification, ct);
    }

    private async Task<User?> FindLeastLoadedUserAsync(string roleName, Guid? preferredStoreId, CancellationToken ct)
    {
        const int maxLeadsPerPerson = 20;

        var query = _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive
                        && u.Role != null
                        && u.Role.RoleName == roleName
                        && u.CurrentWorkload < maxLeadsPerPerson);

        var candidates = await query.ToListAsync(ct);
        if (!candidates.Any()) return null;

        // Prefer users in the same store if specified
        if (preferredStoreId.HasValue)
        {
            var storeMatch = candidates
                .Where(u => u.StoreId == preferredStoreId)
                .OrderBy(u => u.CurrentWorkload)
                .ThenBy(u => u.LastAssignedAt ?? DateTime.MinValue)
                .FirstOrDefault();
            if (storeMatch is not null) return storeMatch;
        }

        // BR-02: lowest workload first; tiebreak by oldest LastAssignedAt
        return candidates
            .OrderBy(u => u.CurrentWorkload)
            .ThenBy(u => u.LastAssignedAt ?? DateTime.MinValue)
            .FirstOrDefault();
    }

    private async Task NotifyRoleUsersAsync(Lead lead, string roleName, string notificationType, string title, string body, CancellationToken ct)
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role != null && u.Role.RoleName == roleName)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            var notification = Notification.Create(
                userId: user.UserId,
                type: notificationType,
                title: title,
                body: body,
                entityType: "LEAD",
                entityId: lead.Id
            );
            await _notificationRepository.AddAsync(notification, ct);
        }
    }

    // -----------------------------------------------------------------------
    // Ticket creation helpers (used when routing to Cskh group)
    // -----------------------------------------------------------------------
    private async Task<Ticket> BuildTicketFromLeadAsync(
        Lead lead, Guid assignedUserId, DateTime slaDeadline, CancellationToken ct)
    {
        var ticketCode = await GenerateTicketCodeAsync(ct);

        var ticket = Ticket.Create(
            ticketCode: ticketCode,
            customerName: lead.CustomerName,
            customerPhone: lead.CustomerPhone,
            channel: lead.Channel,
            needDescription: lead.NeedDescription,
            createdBy: lead.CreatedBy,
            customerAddress: lead.CustomerAddress,
            customerEmail: lead.CustomerEmail,
            leadId: lead.Id
        );

        if (lead.NeedType.HasValue && lead.PriorityLevel.HasValue)
            ticket.SetClassification(lead.NeedType.Value, lead.PriorityScore, lead.PriorityLevel.Value);

        ticket.SetSystemAssignment(assignedUserId, slaDeadline, lead.AssignedStoreId);

        return ticket;
    }

    private async Task<string> GenerateTicketCodeAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"TK{today}";

        var maxCode = await _context.Tickets
            .Where(x => x.TicketCode.StartsWith(prefix))
            .OrderByDescending(x => x.TicketCode)
            .Select(x => x.TicketCode)
            .FirstOrDefaultAsync(ct);

        int seq = 1;
        if (maxCode is not null && maxCode.Length > prefix.Length
            && int.TryParse(maxCode[prefix.Length..], out var last))
        {
            seq = last + 1;
        }

        return $"{prefix}{seq:D3}";
    }
}
