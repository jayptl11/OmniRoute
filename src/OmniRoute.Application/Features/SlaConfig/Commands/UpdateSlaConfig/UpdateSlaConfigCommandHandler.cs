using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.SlaConfig.Commands.UpdateSlaConfig;

internal sealed class UpdateSlaConfigCommandHandler : ICommandHandler<UpdateSlaConfigCommand>
{
    private readonly ISlaConfigRepository _repository;
    private readonly IApplicationDbContext _db;

    public UpdateSlaConfigCommandHandler(ISlaConfigRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(UpdateSlaConfigCommand command, CancellationToken ct)
    {
        var slaConfig = await _repository.GetByIdAsync(command.Id, ct);
        if (slaConfig is null)
            return Result.Failure("NOT_FOUND", "SLA config not found.");

        if (command.WarningBeforeHours >= command.MaxHours)
            return Result.Failure("INVALID_WARNING_HOURS", "WarningBeforeHours must be less than MaxHours.");

        slaConfig.Update(command.MaxHours, command.WarningBeforeHours);
        await _repository.UpdateAsync(slaConfig, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
