using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Stores.Commands.UpdateStore;

internal sealed class UpdateStoreCommandHandler : ICommandHandler<UpdateStoreCommand>
{
    private readonly IStoreRepository _repository;
    private readonly IApplicationDbContext _db;

    public UpdateStoreCommandHandler(IStoreRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(UpdateStoreCommand command, CancellationToken ct)
    {
        var store = await _repository.GetByIdAsync(command.Id, ct);
        if (store is null)
            return Result.Failure("NOT_FOUND", "Store not found.");

        store.Update(command.StoreName, command.MaxCapacity, command.Address, command.Region, command.ManagerId);
        await _repository.UpdateAsync(store, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
