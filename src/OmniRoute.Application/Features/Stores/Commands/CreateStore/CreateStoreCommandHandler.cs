using Microsoft.EntityFrameworkCore;
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

        // Resolve manager by username (optional)
        User? manager = null;
        if (!string.IsNullOrWhiteSpace(command.ManagerUsername))
        {
            manager = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == command.ManagerUsername, ct);

            if (manager is null)
                return Result<StoreDto>.Failure("MANAGER_NOT_FOUND",
                    $"Không tìm thấy người dùng '{command.ManagerUsername}'.");

            if (manager.Role?.RoleName != "QL")
                return Result<StoreDto>.Failure("INVALID_MANAGER_ROLE",
                    "Chỉ có thể gán người dùng có role QL làm quản lý đơn vị.");

            if (!manager.IsActive)
                return Result<StoreDto>.Failure("MANAGER_INACTIVE",
                    "Người dùng đã bị khóa, không thể gán làm quản lý đơn vị.");
        }

        var store = Store.Create(
            command.StoreCode,
            command.StoreName,
            command.MaxCapacity,
            command.Address,
            command.Region,
            manager?.UserId);

        await _repository.AddAsync(store, ct);

        // Assign store to manager user (bidirectional)
        if (manager is not null)
            manager.AssignToStore(store.Id);

        await _db.SaveChangesAsync(ct);

        return Result<StoreDto>.Success(new StoreDto(
            store.Id, store.StoreCode, store.StoreName, store.Address,
            store.Region, store.ManagerId,
            manager is null ? null : $"{manager.FirstName} {manager.LastName}".Trim(),
            manager?.Username,
            store.MaxCapacity, store.IsActive, store.CreatedAt));
    }
}

