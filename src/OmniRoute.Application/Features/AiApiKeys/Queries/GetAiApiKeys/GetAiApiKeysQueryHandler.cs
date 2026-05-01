using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.AiApiKeys.Queries.GetAiApiKeys;

internal sealed class GetAiApiKeysQueryHandler : IQueryHandler<GetAiApiKeysQuery, List<AiApiKeyDto>>
{
    private readonly IAiApiKeyRepository _repository;

    public GetAiApiKeysQueryHandler(IAiApiKeyRepository repository) => _repository = repository;

    public async Task<Result<List<AiApiKeyDto>>> Handle(GetAiApiKeysQuery query, CancellationToken ct)
    {
        var keys = await _repository.GetAllAsync(ct);

        var dtos = keys
            .OrderBy(k => k.Priority)
            .ThenBy(k => k.Provider)
            .Select(k => new AiApiKeyDto(
                k.Id,
                k.Provider,
                k.DisplayName,
                MaskKey(k.EncryptedKey),
                k.Priority,
                k.IsActive,
                k.FailureCount,
                k.LastFailedAt,
                k.LastUsedAt,
                k.CreatedAt))
            .ToList();

        return Result<List<AiApiKeyDto>>.Success(dtos);
    }

    // Show only last 4 chars of the encrypted key (safe for display — never decrypt for listing)
    private static string MaskKey(string encryptedKey)
    {
        if (encryptedKey.Length <= 4)
            return "****";
        return $"****{encryptedKey[^4..]}";
    }
}
