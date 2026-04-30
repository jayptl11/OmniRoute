using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.NotificationConfigs.DTOs;

namespace OmniRoute.Application.Features.NotificationConfigs.Queries.GetNotificationConfigs;

public record GetNotificationConfigsQuery : IQuery<List<NotificationConfigDto>>;
