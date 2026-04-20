using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.MasterData.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.MasterData.Commands.CreateMasterDataItem;

internal sealed class CreateMasterDataItemCommandHandler
    : ICommandHandler<CreateMasterDataItemCommand, MasterDataItemDto>
{
    private readonly IMasterDataRepository _repository;
    private readonly IApplicationDbContext _db;

    public CreateMasterDataItemCommandHandler(
        IMasterDataRepository repository,
        IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result<MasterDataItemDto>> Handle(
        CreateMasterDataItemCommand command,
        CancellationToken ct)
    {
        var codeExists = await _repository.ExistsByCodeAsync(command.Category, command.Code, null, ct);
        if (codeExists)
            return Result<MasterDataItemDto>.Failure("CODE_TAKEN", $"Code '{command.Code}' already exists in category '{command.Category}'.");

        var item = MasterDataItem.Create(
            command.Category,
            command.Code,
            command.DisplayName,
            command.Description,
            command.SortOrder);

        await _repository.AddAsync(item, ct);
        await _db.SaveChangesAsync(ct);

        return Result<MasterDataItemDto>.Success(new MasterDataItemDto(
            item.Id,
            item.Category.ToString(),
            item.Code,
            item.DisplayName,
            item.Description,
            item.SortOrder,
            item.IsActive,
            item.CreatedAt));
    }
}
