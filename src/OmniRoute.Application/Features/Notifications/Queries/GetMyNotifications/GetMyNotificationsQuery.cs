using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Notifications.DTOs;

namespace OmniRoute.Application.Features.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(int Page, int PageSize) : IQuery<GetNotificationsResponse>;
