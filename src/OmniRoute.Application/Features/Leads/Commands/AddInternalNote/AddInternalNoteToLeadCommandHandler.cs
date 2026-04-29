using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.AddInternalNote;

internal sealed class AddInternalNoteToLeadCommandHandler : ICommandHandler<AddInternalNoteToLeadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddInternalNoteToLeadCommandHandler(
        IApplicationDbContext db,
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AddInternalNoteToLeadCommand command, CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;
        if (teamId is null)
            return Result.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var lead = await _leadRepository.GetByIdAsync(command.LeadId, ct);
        if (lead is null)
            return Result.Failure("LEAD_NOT_FOUND", "Không tìm thấy lead.");

        // Lead phải thuộc team của TN (được gán cho thành viên trong đội TN)
        // Hoặc lead chưa gán nhưng đang ở PendingDispatch/PendingAssignment (vẫn trong phạm vi giám sát)
        bool isInScope;
        if (lead.AssignedUserId.HasValue)
        {
            isInScope = await _db.Users
                .AnyAsync(u => u.UserId == lead.AssignedUserId && u.TeamId == teamId, ct);
        }
        else
        {
            // Lead chưa gán: kiểm tra CreatedBy thuộc team
            isInScope = await _db.Users
                .AnyAsync(u => u.UserId == lead.CreatedBy && u.TeamId == teamId, ct);
        }

        if (!isInScope)
            return Result.Failure("LEAD_NOT_IN_TEAM", "Lead này không thuộc phạm vi đội của bạn.");

        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "INTERNAL_NOTE",
            performedBy: _currentUserService.GetUserId(),
            note: command.Content,
            isInternal: true);

        await _activityLogRepository.AddAsync(log, ct);

        return Result.Success();
    }
}
