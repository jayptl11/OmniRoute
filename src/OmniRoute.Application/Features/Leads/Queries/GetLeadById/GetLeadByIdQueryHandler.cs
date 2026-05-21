using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.Leads.Queries.GetLeadById;

internal sealed class GetLeadByIdQueryHandler
    : IQueryHandler<GetLeadByIdQuery, LeadDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetLeadByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<LeadDetailDto>> Handle(
        GetLeadByIdQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _db.Leads
            .AsNoTracking()
            .Include(l => l.AssignedUser)
            .Where(l => l.Id == query.LeadId && l.CreatedBy == currentUserId)
            .FirstOrDefaultAsync(ct);

        if (lead is null)
        {
            return Result<LeadDetailDto>.Failure("NOT_FOUND", "Lead không tồn tại hoặc bạn không có quyền xem.");
        }

        List<string>? productInterest = null;
        if (!string.IsNullOrEmpty(lead.ProductInterest))
        {
            productInterest = JsonSerializer.Deserialize<List<string>>(lead.ProductInterest);
        }

        string? assignedUserName = null;
        if (lead.AssignedUser is not null)
        {
            var firstName = lead.AssignedUser.FirstName;
            var lastName = lead.AssignedUser.LastName;
            assignedUserName = string.IsNullOrWhiteSpace($"{firstName} {lastName}".Trim())
                ? lead.AssignedUser.Username
                : $"{firstName} {lastName}".Trim();
        }

        var dto = new LeadDetailDto(
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
            ClosedAt: lead.ClosedAt);

        return Result<LeadDetailDto>.Success(dto);
    }
}
