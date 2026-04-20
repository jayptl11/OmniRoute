using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Stores.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Stores.Commands.CreateStore;

internal sealed class CreateStoreCommandHandler : ICommandHandler<CreateStoreCommand, StoreDto>
{
    private readonly IStoreRepository _repository;
    private readonly IApplicationDbContext _db;

    public CreateStoreCommandHandler(IStoreRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result<StoreDto>> Handle(CreateStoreCommand command, CancellationToken ct)
    {
        var codeExists = await _repository.ExistsByCodeAsync(command.StoreCode, null, ct);
        if (codeExists)
            return Result<StoreDto>.Failure("CODE_TAKEN", $"Store code '{command.StoreCode}' is already in use.");

        var store = Store.Create(
            command.StoreCode,
            command.StoreName,
            command.MaxCapacity,
            command.Address,
            command.Region,
            command.ManagerId);

        await _repository.AddAsync(store, ct);
        await _db.SaveChangesAsync(ct);

        return Result<StoreDto>.Success(new StoreDto(
            store.Id, store.StoreCode, store.StoreName, store.Address,
            store.Region, store.ManagerId, store.MaxCapacity, store.IsActive, store.CreatedAt));
    }
}
