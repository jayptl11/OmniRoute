using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.Leads.Queries.GetPendingDispatchLeadById;

internal sealed class GetPendingDispatchLeadByIdQueryHandler
    : IQueryHandler<GetPendingDispatchLeadByIdQuery, PendingDispatchLeadDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetPendingDispatchLeadByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PendingDispatchLeadDetailDto>> Handle(
        GetPendingDispatchLeadByIdQuery query,
        CancellationToken ct)
    {
        // DP-02: Chỉ xem lead đang ở trạng thái PendingDispatch.
        var lead = await _db.Leads
            .AsNoTracking()
            .Where(l => l.Id == query.LeadId && l.Status == LeadStatus.PendingDispatch)
            .FirstOrDefaultAsync(ct);

        if (lead is null)
        {
            return Result<PendingDispatchLeadDetailDto>.Failure(
                "NOT_FOUND", "Lead không tồn tại hoặc không ở trạng thái chờ điều phối.");
        }

        List<string>? productInterest = null;
        if (!string.IsNullOrEmpty(lead.ProductInterest))
        {
            productInterest = JsonSerializer.Deserialize<List<string>>(lead.ProductInterest);
        }

        var activityLogs = await _db.ActivityLogs
            .AsNoTracking()
            .Include(al => al.PerformedByUser)
            .Where(al => al.EntityType == "LEAD" && al.EntityId == lead.Id)
            .OrderBy(al => al.PerformedAt)
            .Select(al => new ActivityLogItemDto(
                al.Id,
                al.Action,
                al.Note,
                al.NewValue,
                al.PerformedAt,
                al.PerformedByUser != null
                    ? ($"{al.PerformedByUser.FirstName} {al.PerformedByUser.LastName}".Trim() != string.Empty
                        ? $"{al.PerformedByUser.FirstName} {al.PerformedByUser.LastName}".Trim()
                        : al.PerformedByUser.Username)
                    : null))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var dto = new PendingDispatchLeadDetailDto(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            CustomerName: lead.CustomerName,
            CustomerPhone: lead.CustomerPhone,
            CustomerAddress: lead.CustomerAddress,
            CustomerEmail: lead.CustomerEmail,
            Channel: RoutingRuleChannelHelper.GetCanonicalName(lead.Channel),
            ChannelDisplayName: RoutingRuleChannelHelper.GetDisplayName(lead.Channel),
            NeedDescription: lead.NeedDescription,
            ProductInterest: productInterest,
            NeedType: lead.NeedType?.ToString(),
            PriorityScore: lead.PriorityScore,
            PriorityLevel: lead.PriorityLevel?.ToString(),
            AssignedGroup: lead.AssignedGroup?.ToString(),
            WaitedMinutes: (int)(now - lead.CreatedAt).TotalMinutes,
            CreatedAt: lead.CreatedAt,
            UpdatedAt: lead.UpdatedAt,
            ActivityLogs: activityLogs);

        return Result<PendingDispatchLeadDetailDto>.Success(dto);
    }
}
