using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.NotificationConfigs.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.NotificationConfigs.Queries.GetNotificationConfigs;

internal sealed class GetNotificationConfigsQueryHandler
    : IQueryHandler<GetNotificationConfigsQuery, List<NotificationConfigDto>>
{
    private readonly INotificationConfigRepository _repository;

    public GetNotificationConfigsQueryHandler(INotificationConfigRepository repository)
        => _repository = repository;

    public async Task<Result<List<NotificationConfigDto>>> Handle(
        GetNotificationConfigsQuery query,
        CancellationToken ct)
    {
        var configs = await _repository.GetAllAsync(ct);
        var dtos = configs
            .Select(c => new NotificationConfigDto(c.Id, c.NotificationType, c.TargetRole, c.IsEnabled, c.UpdatedAt))
            .ToList();

        return Result<List<NotificationConfigDto>>.Success(dtos);
    }
}
