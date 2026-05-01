using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.AddAiApiKey;

internal sealed class AddAiApiKeyCommandHandler : ICommandHandler<AddAiApiKeyCommand, Guid>
{
    private readonly IAiApiKeyRepository _repository;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly IApplicationDbContext _db;

    public AddAiApiKeyCommandHandler(
        IAiApiKeyRepository repository,
        IAiKeyEncryptionService encryption,
        IApplicationDbContext db)
    {
        _repository = repository;
        _encryption = encryption;
        _db = db;
    }

    public async Task<Result<Guid>> Handle(AddAiApiKeyCommand command, CancellationToken ct)
    {
        var encryptedKey = _encryption.Encrypt(command.PlainKeyValue);

        var key = AiApiKey.Create(
            command.Provider,
            command.DisplayName,
            encryptedKey,
            command.ConfigJson,
            command.Priority,
            command.IsActive);

        await _repository.AddAsync(key, ct);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(key.Id);
    }
}
