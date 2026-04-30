using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.NotificationConfigs.Commands.UpdateNotificationConfig;

internal sealed class UpdateNotificationConfigCommandHandler
    : ICommandHandler<UpdateNotificationConfigCommand>
{
    private readonly INotificationConfigRepository _repository;
    private readonly IApplicationDbContext _db;

    public UpdateNotificationConfigCommandHandler(
        INotificationConfigRepository repository,
        IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(UpdateNotificationConfigCommand command, CancellationToken ct)
    {
        var config = await _repository.GetByIdAsync(command.Id, ct);
        if (config is null)
            return Result.Failure("NOT_FOUND", "Cấu hình thông báo không tồn tại.");

        config.SetEnabled(command.IsEnabled);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
