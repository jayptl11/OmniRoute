using Microsoft.EntityFrameworkCore;
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

        // Resolve new manager by username (optional)
        Guid? newManagerId = null;
        if (!string.IsNullOrWhiteSpace(command.ManagerUsername))
        {
            var newManager = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == command.ManagerUsername, ct);

            if (newManager is null)
                return Result.Failure("MANAGER_NOT_FOUND",
                    $"Không tìm thấy người dùng '{command.ManagerUsername}'.");

            if (newManager.Role?.RoleName != "QL")
                return Result.Failure("INVALID_MANAGER_ROLE",
                    "Chỉ có thể gán người dùng có role QL làm quản lý đơn vị.");

            if (!newManager.IsActive)
                return Result.Failure("MANAGER_INACTIVE",
                    "Người dùng đã bị khóa, không thể gán làm quản lý đơn vị.");

            // If changing manager, clear old manager's StoreId
            if (store.ManagerId.HasValue && store.ManagerId != newManager.UserId)
            {
                var oldManager = await _db.Users.FirstOrDefaultAsync(
                    u => u.UserId == store.ManagerId, ct);
                oldManager?.AssignToStore(null);
            }

            newManager.AssignToStore(store.Id);
            newManagerId = newManager.UserId;
        }
        else
        {
            // ManagerUsername is null/empty → clear manager
            if (store.ManagerId.HasValue)
            {
                var oldManager = await _db.Users.FirstOrDefaultAsync(
                    u => u.UserId == store.ManagerId, ct);
                oldManager?.AssignToStore(null);
            }
        }

        store.Update(command.StoreName, command.MaxCapacity, command.Address, command.Region, newManagerId);
        await _repository.UpdateAsync(store, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

