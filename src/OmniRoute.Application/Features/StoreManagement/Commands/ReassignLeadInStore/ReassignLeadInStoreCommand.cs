using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.StoreManagement.Commands.ReassignLeadInStore;

public record ReassignLeadInStoreCommand(Guid LeadId, Guid NewUserId, string Reason) : ICommand;
