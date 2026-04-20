using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.MasterData.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.MasterData.Commands.CreateMasterDataItem;

public record CreateMasterDataItemCommand(
    MasterDataCategory Category,
    string Code,
    string DisplayName,
    string? Description,
    int SortOrder) : ICommand<MasterDataItemDto>;
