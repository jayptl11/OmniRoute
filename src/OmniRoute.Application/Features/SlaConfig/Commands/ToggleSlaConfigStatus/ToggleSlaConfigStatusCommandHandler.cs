using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.SlaConfig.Commands.ToggleSlaConfigStatus;

internal sealed class ToggleSlaConfigStatusCommandHandler : ICommandHandler<ToggleSlaConfigStatusCommand>
{
    private readonly ISlaConfigRepository _repository;
    private readonly IApplicationDbContext _db;

    public ToggleSlaConfigStatusCommandHandler(ISlaConfigRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(ToggleSlaConfigStatusCommand command, CancellationToken ct)
    {
        var slaConfig = await _repository.GetByIdAsync(command.Id, ct);
        if (slaConfig is null)
            return Result.Failure("NOT_FOUND", "SLA config not found.");

        if (command.IsActive)
            slaConfig.Activate();
        else
            slaConfig.Deactivate();

        await _repository.UpdateAsync(slaConfig, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
