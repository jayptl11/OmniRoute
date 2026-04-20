using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.MasterData.Commands.ToggleMasterDataItemStatus;

public record ToggleMasterDataItemStatusCommand(Guid Id, bool IsActive) : ICommand;
