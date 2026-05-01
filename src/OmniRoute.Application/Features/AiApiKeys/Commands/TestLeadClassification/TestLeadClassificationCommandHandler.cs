using System.Diagnostics;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.TestLeadClassification;

internal sealed class TestLeadClassificationCommandHandler
    : ICommandHandler<TestLeadClassificationCommand, TestLeadClassificationResult>
{
    private readonly IAiApiKeyRepository _repository;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly IAiClassificationService _aiService;

    public TestLeadClassificationCommandHandler(
        IAiApiKeyRepository repository,
        IAiKeyEncryptionService encryption,
        IAiClassificationService aiService)
    {
        _repository = repository;
        _encryption = encryption;
        _aiService = aiService;
    }

    public async Task<Result<TestLeadClassificationResult>> Handle(
        TestLeadClassificationCommand command, CancellationToken ct)
    {
        var key = await _repository.GetByIdAsync(command.Id, ct);
        if (key is null)
            return Result<TestLeadClassificationResult>.Failure("NOT_FOUND",
                $"AI API key '{command.Id}' not found.");

        if (!Enum.TryParse<Channel>(command.Channel, ignoreCase: true, out var channel))
            return Result<TestLeadClassificationResult>.Failure("INVALID_CHANNEL",
                $"Channel '{command.Channel}' is not valid. Valid values: {string.Join(", ", Enum.GetNames<Channel>())}");

        var plainKey = _encryption.Decrypt(key.EncryptedKey);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _aiService.ClassifyWithKeyAsync(
                key.Provider, plainKey, key.ConfigJson,
                command.NeedDescription, channel.ToString(), ct);
            sw.Stop();

            var assignedGroup = MapNeedTypeToGroup(result.NeedType);

            return Result<TestLeadClassificationResult>.Success(
                new TestLeadClassificationResult(
                    Success: true,
                    NeedType: result.NeedType.ToString(),
                    ConfidenceScore: result.ConfidenceScore,
                    Reasoning: result.Reasoning,
                    AssignedGroup: assignedGroup.ToString(),
                    Provider: result.UsedProvider,
                    LatencyMs: sw.ElapsedMilliseconds,
                    ErrorMessage: null));
        }
        catch (Exception ex)
        {
            sw.Stop();

            return Result<TestLeadClassificationResult>.Success(
                new TestLeadClassificationResult(
                    Success: false,
                    NeedType: null,
                    ConfidenceScore: 0,
                    Reasoning: string.Empty,
                    AssignedGroup: null,
                    Provider: key.Provider,
                    LatencyMs: sw.ElapsedMilliseconds,
                    ErrorMessage: ex.Message));
        }
    }

    private static AssignedGroup MapNeedTypeToGroup(NeedType needType) => needType switch
    {
        NeedType.SaleNew or NeedType.SaleUpgrade or NeedType.SaleRenew => AssignedGroup.Sale,
        NeedType.CskhSupport or NeedType.CskhComplaint or NeedType.CskhWarranty => AssignedGroup.Cskh,
        _ => AssignedGroup.StoreSupport
    };
}
