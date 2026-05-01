using System.Diagnostics;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.TestAiApiKey;

internal sealed class TestAiApiKeyCommandHandler : ICommandHandler<TestAiApiKeyCommand, TestAiApiKeyResult>
{
    private readonly IAiApiKeyRepository _repository;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly IAiClassificationService _aiService;
    private readonly IApplicationDbContext _db;

    public TestAiApiKeyCommandHandler(
        IAiApiKeyRepository repository,
        IAiKeyEncryptionService encryption,
        IAiClassificationService aiService,
        IApplicationDbContext db)
    {
        _repository = repository;
        _encryption = encryption;
        _aiService = aiService;
        _db = db;
    }

    public async Task<Result<TestAiApiKeyResult>> Handle(TestAiApiKeyCommand command, CancellationToken ct)
    {
        var key = await _repository.GetByIdAsync(command.Id, ct);
        if (key is null)
            return Result<TestAiApiKeyResult>.Failure("NOT_FOUND", $"AI API key '{command.Id}' not found.");

        var plainKey = _encryption.Decrypt(key.EncryptedKey);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _aiService.ClassifyWithKeyAsync(key.Provider, plainKey, key.ConfigJson, "test ping", "Hotline", ct);
            sw.Stop();

            key.RecordSuccess();
            await _repository.UpdateAsync(key, ct);
            await _db.SaveChangesAsync(ct);

            return Result<TestAiApiKeyResult>.Success(
                new TestAiApiKeyResult(true, null, sw.ElapsedMilliseconds, key.Provider));
        }
        catch (Exception ex)
        {
            sw.Stop();

            key.RecordFailure();
            await _repository.UpdateAsync(key, ct);
            await _db.SaveChangesAsync(ct);

            return Result<TestAiApiKeyResult>.Success(
                new TestAiApiKeyResult(false, ex.Message, sw.ElapsedMilliseconds, key.Provider));
        }
    }
}
