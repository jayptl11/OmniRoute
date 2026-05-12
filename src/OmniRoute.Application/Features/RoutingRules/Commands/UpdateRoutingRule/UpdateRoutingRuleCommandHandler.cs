using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.RoutingRules.Commands.UpdateRoutingRule;

internal sealed class UpdateRoutingRuleCommandHandler : ICommandHandler<UpdateRoutingRuleCommand>
{
    private readonly IRoutingRuleRepository _repository;
    private readonly IApplicationDbContext _db;

    public UpdateRoutingRuleCommandHandler(IRoutingRuleRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(UpdateRoutingRuleCommand command, CancellationToken ct)
    {
        var rule = await _repository.GetByIdAsync(command.Id, ct);
        if (rule is null)
            return Result.Failure("NOT_FOUND", "Routing rule not found.");

        var isTaken = await _repository.IsPriorityOrderTakenAsync(command.PriorityOrder, excludeId: command.Id, ct);
        if (isTaken)
            return Result.Failure("DUPLICATE_PRIORITY_ORDER",
                $"Priority order {command.PriorityOrder} is already used by another rule.");

        var normalizedChannels = RoutingRuleChannelHelper.NormalizeConditionChannels(command.ConditionChannels);
        var channelJson = normalizedChannels is { Count: > 0 }
            ? JsonSerializer.Serialize(normalizedChannels)
            : null;

        var keywordsJson = command.ConditionKeywords is { Count: > 0 }
            ? JsonSerializer.Serialize(command.ConditionKeywords)
            : null;

        rule.Update(
            command.RuleName,
            command.PriorityOrder,
            command.ActionGroup,
            command.Description,
            channelJson,
            keywordsJson,
            command.ActionTeamId);

        await _repository.UpdateAsync(rule, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
