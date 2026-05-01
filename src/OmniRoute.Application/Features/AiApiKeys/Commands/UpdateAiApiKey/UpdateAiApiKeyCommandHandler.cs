using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.UpdateAiApiKey;

internal sealed class UpdateAiApiKeyCommandHandler : ICommandHandler<UpdateAiApiKeyCommand>
{
    private readonly IAiApiKeyRepository _repository;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly IApplicationDbContext _db;

    public UpdateAiApiKeyCommandHandler(
        IAiApiKeyRepository repository,
        IAiKeyEncryptionService encryption,
        IApplicationDbContext db)
    {
        _repository = repository;
        _encryption = encryption;
        _db = db;
    }

    public async Task<Result> Handle(UpdateAiApiKeyCommand command, CancellationToken ct)
    {
        var key = await _repository.GetByIdAsync(command.Id, ct);
        if (key is null)
            return Result.Failure("NOT_FOUND", $"AI API key '{command.Id}' not found.");

        if (command.PlainKeyValue is not null)
        {
            var encryptedKey = _encryption.Encrypt(command.PlainKeyValue);
            key.UpdateKey(encryptedKey, command.DisplayName, command.Priority);
        }
        else
        {
            key.UpdateMeta(command.DisplayName, command.Priority);
        }

        await _repository.UpdateAsync(key, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
