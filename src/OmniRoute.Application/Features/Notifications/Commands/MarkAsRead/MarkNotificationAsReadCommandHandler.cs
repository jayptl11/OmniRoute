using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Notifications.Commands.MarkAsRead;

internal sealed class MarkNotificationAsReadCommandHandler
    : ICommandHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _db;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext db)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _db = db;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.GetUserId();

        var notification = await _notificationRepository.GetByIdAsync(command.NotificationId, ct);
        if (notification is null)
            return Result.Failure("NOT_FOUND", "Thông báo không tồn tại.");

        if (notification.UserId != userId)
            return Result.Failure("FORBIDDEN", "Bạn không có quyền cập nhật thông báo này.");

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
