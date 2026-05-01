using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.AiApiKeys.Queries.GetAiApiKeys;

public record AiApiKeyDto(
    Guid Id,
    string Provider,
    string DisplayName,
    string MaskedKey,
    int Priority,
    bool IsActive,
    JsonElement Config,
    int FailureCount,
    DateTime? LastFailedAt,
    DateTime? LastUsedAt,
    DateTime CreatedAt
);

public record GetAiApiKeysQuery : IQuery<List<AiApiKeyDto>>;
