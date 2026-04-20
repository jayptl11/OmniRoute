using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Stores.Commands.ToggleStoreStatus;

public record ToggleStoreStatusCommand(Guid Id, bool IsActive) : ICommand;
