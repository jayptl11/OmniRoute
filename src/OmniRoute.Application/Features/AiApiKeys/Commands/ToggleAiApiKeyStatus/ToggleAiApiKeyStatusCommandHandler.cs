using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.ToggleAiApiKeyStatus;

internal sealed class ToggleAiApiKeyStatusCommandHandler : ICommandHandler<ToggleAiApiKeyStatusCommand>
{
    private readonly IAiApiKeyRepository _repository;
    private readonly IApplicationDbContext _db;

    public ToggleAiApiKeyStatusCommandHandler(IAiApiKeyRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(ToggleAiApiKeyStatusCommand command, CancellationToken ct)
    {
        var key = await _repository.GetByIdAsync(command.Id, ct);
        if (key is null)
            return Result.Failure("NOT_FOUND", $"AI API key '{command.Id}' not found.");

        if (key.IsActive)
            key.Deactivate();
        else
            key.Activate();

        await _repository.UpdateAsync(key, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
