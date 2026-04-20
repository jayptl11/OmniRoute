using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.MasterData.Commands.UpdateMasterDataItem;

public record UpdateMasterDataItemCommand(
    Guid Id,
    string DisplayName,
    string? Description,
    int SortOrder) : ICommand;
