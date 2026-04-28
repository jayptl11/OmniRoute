using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.UpdateLead;

internal sealed class UpdateLeadCommandHandler
    : ICommandHandler<UpdateLeadCommand, UpdateLeadResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateLeadCommandHandler(
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UpdateLeadResponse>> Handle(
        UpdateLeadCommand command,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);

        if (lead is null || lead.CreatedBy != currentUserId)
            return Result<UpdateLeadResponse>.Failure("NOT_FOUND", "Lead không tồn tại hoặc bạn không có quyền chỉnh sửa.");

        // Không cho sửa lead đã ở trạng thái kết thúc
        if (lead.Status is LeadStatus.Won or LeadStatus.Lost or LeadStatus.Cancelled)
            return Result<UpdateLeadResponse>.Failure(
                "LEAD_CLOSED",
                "Không thể chỉnh sửa lead đã đóng (Won/Lost/Cancelled).");

        // Capture old value để ghi log
        var oldValue = JsonSerializer.Serialize(new
        {
            CustomerAddress = lead.CustomerAddress,
            CustomerEmail = lead.CustomerEmail,
            ProductInterest = lead.ProductInterest,
            NeedDescription = lead.NeedDescription
        });

        // Serialize product interest nếu có
        string? productInterestJson = command.ProductInterest is { Count: > 0 }
            ? JsonSerializer.Serialize(command.ProductInterest)
            : command.ProductInterest is not null ? null : (string?)null;

        lead.UpdateDetails(
            customerAddress: command.CustomerAddress,
            customerEmail: command.CustomerEmail,
            productInterest: productInterestJson,
            needDescription: command.NeedDescription);

        await _leadRepository.UpdateAsync(lead, ct);

        var newValue = JsonSerializer.Serialize(new
        {
            CustomerAddress = lead.CustomerAddress,
            CustomerEmail = lead.CustomerEmail,
            ProductInterest = lead.ProductInterest,
            NeedDescription = lead.NeedDescription
        });

        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "LEAD_UPDATED",
            performedBy: currentUserId,
            oldValue: oldValue,
            newValue: newValue);

        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result<UpdateLeadResponse>.Success(new UpdateLeadResponse(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            UpdatedAt: lead.UpdatedAt));
    }
}
