using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.MasterData.Commands.UpdateMasterDataItem;

internal sealed class UpdateMasterDataItemCommandHandler : ICommandHandler<UpdateMasterDataItemCommand>
{
    private readonly IMasterDataRepository _repository;
    private readonly IApplicationDbContext _db;

    public UpdateMasterDataItemCommandHandler(IMasterDataRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(UpdateMasterDataItemCommand command, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(command.Id, ct);
        if (item is null)
            return Result.Failure("NOT_FOUND", "Master data item not found.");

        item.Update(command.DisplayName, command.Description, command.SortOrder);
        await _repository.UpdateAsync(item, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
