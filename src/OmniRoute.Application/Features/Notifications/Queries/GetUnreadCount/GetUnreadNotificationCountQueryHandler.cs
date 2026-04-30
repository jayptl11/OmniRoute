using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Notifications.Queries.GetUnreadCount;

internal sealed class GetUnreadNotificationCountQueryHandler
    : IQueryHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationCountQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<int>> Handle(
        GetUnreadNotificationCountQuery query,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetUserId();
        var count = await _notificationRepository.GetUnreadCountAsync(userId, ct);
        return Result<int>.Success(count);
    }
}
