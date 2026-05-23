using FluentAssertions;
using Moq;
using OmniRoute.Application.Common.DTOs;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreReassignTargets;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.UnitTests.Helpers;

namespace OmniRoute.UnitTests.Features.StoreManagement;

public class GetStoreReassignTargetsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyActiveUsersInSameStoreExceptCurrentAssignee()
    {
        var storeId = Guid.NewGuid();
        var assignedUser = CreateUser("assigned", "Assigned", "User", RoleCatalog.StoreSales, storeId: storeId);
        var eligibleUser = CreateUser("target", "Target", "User", RoleCatalog.StoreSales, storeId: storeId);
        var inactiveUser = CreateUser("inactive", "Inactive", "User", RoleCatalog.StoreSales, storeId: storeId, isActive: false);
        var otherStoreUser = CreateUser("other", "Other", "User", RoleCatalog.StoreSales, storeId: Guid.NewGuid());

        var lead = Lead.Create("L002", "Customer", "0900000001", Channel.Walkin, "Need help", Guid.NewGuid());
        lead.SetPendingDispatch();
        lead.DispatchToStore(storeId, DateTime.UtcNow.AddHours(1));
        lead.AssignUserAfterDispatch(assignedUser.UserId);

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Users).Returns(new[] { assignedUser, eligibleUser, inactiveUser, otherStoreUser }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.Leads).Returns(new[] { lead }.BuildMockDbSet().Object);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.StoreId).Returns(storeId);

        var handler = new GetStoreReassignTargetsQueryHandler(contextMock.Object, currentUserMock.Object);

        var result = await handler.Handle(new GetStoreReassignTargetsQuery(lead.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[]
        {
            new UserPickerOptionDto(
                eligibleUser.UserId,
                "Target User",
                RoleCatalog.StoreSales,
                RoleCatalog.GetDisplayName(RoleCatalog.StoreSales))
        });
    }

    private static User CreateUser(
        string username,
        string firstName,
        string lastName,
        string roleName,
        Guid? storeId = null,
        bool isActive = true)
    {
        var roleId = Guid.NewGuid();
        var user = User.Create(Guid.NewGuid(), $"{username}@test.com", username, "hash", firstName, lastName, roleId);
        user.Role = new Role { RoleId = roleId, RoleName = roleName };

        if (storeId.HasValue)
        {
            user.AssignToStore(storeId.Value);
        }

        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }
}
