using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Stores.Commands.ToggleStoreStatus;

internal sealed class ToggleStoreStatusCommandHandler : ICommandHandler<ToggleStoreStatusCommand>
{
    private readonly IStoreRepository _repository;
    private readonly IApplicationDbContext _db;

    public ToggleStoreStatusCommandHandler(IStoreRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(ToggleStoreStatusCommand command, CancellationToken ct)
    {
        var store = await _repository.GetByIdAsync(command.Id, ct);
        if (store is null)
            return Result.Failure("NOT_FOUND", "Store not found.");

        if (command.IsActive)
            store.Activate();
        else
            store.Deactivate();

        await _repository.UpdateAsync(store, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
