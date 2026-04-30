using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Notifications.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Notifications.Queries.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler
    : IQueryHandler<GetMyNotificationsQuery, GetNotificationsResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<GetNotificationsResponse>> Handle(
        GetMyNotificationsQuery query,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetUserId();

        var (items, totalCount) = await _notificationRepository
            .GetByUserIdAsync(userId, query.Page, query.PageSize, ct);

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.Type, n.Title, n.Body, n.EntityType, n.EntityId, n.IsRead, n.CreatedAt))
            .ToList();

        return Result<GetNotificationsResponse>.Success(
            new GetNotificationsResponse(dtos, totalCount, query.Page, query.PageSize));
    }
}
