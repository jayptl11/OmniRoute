using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.RoutingRules.Commands.CreateRoutingRule;

internal sealed class CreateRoutingRuleCommandHandler : ICommandHandler<CreateRoutingRuleCommand, Guid>
{
    private readonly IRoutingRuleRepository _repository;
    private readonly IApplicationDbContext _db;

    public CreateRoutingRuleCommandHandler(IRoutingRuleRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateRoutingRuleCommand command, CancellationToken ct)
    {
        var isTaken = await _repository.IsPriorityOrderTakenAsync(command.PriorityOrder, excludeId: null, ct);
        if (isTaken)
            return Result<Guid>.Failure("DUPLICATE_PRIORITY_ORDER",
                $"Priority order {command.PriorityOrder} is already used by another rule.");

        var normalizedChannels = RoutingRuleChannelHelper.NormalizeConditionChannels(command.ConditionChannels);
        var channelJson = normalizedChannels is { Count: > 0 }
            ? JsonSerializer.Serialize(normalizedChannels)
            : null;

        var keywordsJson = command.ConditionKeywords is { Count: > 0 }
            ? JsonSerializer.Serialize(command.ConditionKeywords)
            : null;

        var rule = RoutingRule.Create(
            command.RuleName,
            command.PriorityOrder,
            command.ActionGroup,
            command.Description,
            channelJson,
            keywordsJson,
            command.ActionTeamId);

        await _repository.AddAsync(rule, ct);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(rule.Id);
    }
}
