using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetAssignedLeadById;

internal sealed class GetAssignedLeadByIdQueryHandler
    : IQueryHandler<GetAssignedLeadByIdQuery, SaleLeadDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetAssignedLeadByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SaleLeadDetailDto>> Handle(
        GetAssignedLeadByIdQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        // SA-02: Chỉ lấy lead được gán cho nhân viên hiện tại
        var lead = await _db.Leads
            .AsNoTracking()
            .Include(l => l.AssignedUser)
            .Where(l => l.Id == query.LeadId && l.AssignedUserId == currentUserId)
            .FirstOrDefaultAsync(ct);

        if (lead is null)
            return Result<SaleLeadDetailDto>.Failure("NOT_FOUND", "Lead không tồn tại hoặc chưa được gán cho bạn.");

        List<string>? productInterest = null;
        if (!string.IsNullOrEmpty(lead.ProductInterest))
            productInterest = JsonSerializer.Deserialize<List<string>>(lead.ProductInterest);

        string? assignedUserName = null;
        if (lead.AssignedUser is not null)
        {
            var fullName = $"{lead.AssignedUser.FirstName} {lead.AssignedUser.LastName}".Trim();
            assignedUserName = string.IsNullOrWhiteSpace(fullName)
                ? lead.AssignedUser.Username
                : fullName;
        }

        // Lấy activity log timeline (chronological) — bao gồm STATUS_CHANGED và CONSULTATION_NOTE
        // IsInternal=true (ghi chú nội bộ của TN/QL) không hiển thị cho SA
        var activityLogs = await _db.ActivityLogs
            .AsNoTracking()
            .Include(al => al.PerformedByUser)
            .Where(al => al.EntityType == "LEAD" && al.EntityId == lead.Id && !al.IsInternal)
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

        var dto = new SaleLeadDetailDto(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            CustomerName: lead.CustomerName,
            CustomerPhone: lead.CustomerPhone,
            CustomerAddress: lead.CustomerAddress,
            CustomerEmail: lead.CustomerEmail,
            Channel: lead.Channel.ToString(),
            NeedDescription: lead.NeedDescription,
            ProductInterest: productInterest,
            NeedType: lead.NeedType?.ToString(),
            PriorityScore: lead.PriorityScore,
            PriorityLevel: lead.PriorityLevel?.ToString(),
            AssignedGroup: lead.AssignedGroup?.ToString(),
            RoutingType: lead.RoutingType.ToString(),
            AssignedUserId: lead.AssignedUserId,
            AssignedUserName: assignedUserName,
            AssignedStoreId: lead.AssignedStoreId,
            AssignedAt: lead.AssignedAt,
            SlaDeadline: lead.SlaDeadline,
            SlaViolated: lead.SlaViolated,
            LeadStatus: lead.Status.ToString(),
            CreatedBy: lead.CreatedBy,
            CreatedAt: lead.CreatedAt,
            UpdatedAt: lead.UpdatedAt,
            ClosedAt: lead.ClosedAt,
            ActivityLogs: activityLogs);

        return Result<SaleLeadDetailDto>.Success(dto);
    }
}
