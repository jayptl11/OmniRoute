using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.StoreManagement.Commands.UnassignStoreStaff;

public record UnassignStoreStaffCommand(Guid UserId) : ICommand;
