using System.Text.Json;
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
                ParseConfig(k.ConfigJson),
                k.FailureCount,
                k.LastFailedAt,
                k.LastUsedAt,
                k.CreatedAt))
            .ToList();

        return Result<List<AiApiKeyDto>>.Success(dtos);
    }

    private static string MaskKey(string encryptedKey)
    {
        if (encryptedKey.Length <= 4)
            return "****";
        return $"****{encryptedKey[^4..]}";
    }

    private static JsonElement ParseConfig(string configJson)
    {
        try { return JsonSerializer.Deserialize<JsonElement>(configJson); }
        catch { return JsonSerializer.Deserialize<JsonElement>("{}"); }
    }
}
