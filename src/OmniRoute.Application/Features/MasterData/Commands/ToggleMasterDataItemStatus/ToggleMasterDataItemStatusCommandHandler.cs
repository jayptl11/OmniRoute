using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.MasterData.Commands.ToggleMasterDataItemStatus;

internal sealed class ToggleMasterDataItemStatusCommandHandler : ICommandHandler<ToggleMasterDataItemStatusCommand>
{
    private readonly IMasterDataRepository _repository;
    private readonly IApplicationDbContext _db;

    public ToggleMasterDataItemStatusCommandHandler(IMasterDataRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(ToggleMasterDataItemStatusCommand command, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(command.Id, ct);
        if (item is null)
            return Result.Failure("NOT_FOUND", "Master data item not found.");

        if (command.IsActive)
            item.Activate();
        else
            item.Deactivate();

        await _repository.UpdateAsync(item, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
