using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.RoutingRules.Commands.ToggleRoutingRuleStatus;

internal sealed class ToggleRoutingRuleStatusCommandHandler : ICommandHandler<ToggleRoutingRuleStatusCommand>
{
    private readonly IRoutingRuleRepository _repository;
    private readonly IApplicationDbContext _db;

    public ToggleRoutingRuleStatusCommandHandler(IRoutingRuleRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(ToggleRoutingRuleStatusCommand command, CancellationToken ct)
    {
        var rule = await _repository.GetByIdAsync(command.Id, ct);
        if (rule is null)
            return Result.Failure("NOT_FOUND", "Routing rule not found.");

        if (command.IsActive)
            rule.Activate();
        else
            rule.Deactivate();

        await _repository.UpdateAsync(rule, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
