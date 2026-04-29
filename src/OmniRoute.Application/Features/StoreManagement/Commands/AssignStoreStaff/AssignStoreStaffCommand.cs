using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.StoreManagement.Commands.AssignStoreStaff;

public record AssignStoreStaffCommand(Guid UserId) : ICommand;
