using FluentAssertions;
using Moq;
using OmniRoute.Application.Common.DTOs;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Features.StoreManagement.Queries.SearchStoreLeadHistoryActors;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.UnitTests.Helpers;

namespace OmniRoute.UnitTests.Features.StoreManagement;

public class SearchStoreLeadHistoryActorsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsHistoricalActorsIncludingInactiveUsers()
    {
        var storeId = Guid.NewGuid();
        var activeActor = CreateUser("active", "Active", "Actor", RoleCatalog.StoreSales, isActive: true);
        var inactiveActor = CreateUser("former", "Former", "Actor", RoleCatalog.StoreSales, isActive: false);
        var nonActor = CreateUser("other", "Other", "Person", RoleCatalog.StoreSales, isActive: true);

        var lead = Lead.Create("L003", "Customer", "0900000002", Channel.Email, "Need help", Guid.NewGuid());
        lead.SetPendingDispatch();
        lead.DispatchToStore(storeId, DateTime.UtcNow.AddHours(1));

        var logs = new[]
        {
            ActivityLog.Create("LEAD", lead.Id, "UPDATED", performedBy: activeActor.UserId),
            ActivityLog.Create("LEAD", lead.Id, "FOLLOW_UP", performedBy: inactiveActor.UserId),
            ActivityLog.Create("LEAD", Guid.NewGuid(), "UPDATED", performedBy: nonActor.UserId)
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Leads).Returns(new[] { lead }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.ActivityLogs).Returns(logs.BuildMockDbSet().Object);
        contextMock.Setup(x => x.Users).Returns(new[] { activeActor, inactiveActor, nonActor }.BuildMockDbSet().Object);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.StoreId).Returns(storeId);

        var handler = new SearchStoreLeadHistoryActorsQueryHandler(contextMock.Object, currentUserMock.Object);

        var result = await handler.Handle(new SearchStoreLeadHistoryActorsQuery("former"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[]
        {
            new UserPickerOptionDto(
                inactiveActor.UserId,
                "Former Actor",
                RoleCatalog.StoreSales,
                RoleCatalog.GetDisplayName(RoleCatalog.StoreSales))
        });
    }

    private static User CreateUser(
        string username,
        string firstName,
        string lastName,
        string roleName,
        bool isActive)
    {
        var roleId = Guid.NewGuid();
        var user = User.Create(Guid.NewGuid(), $"{username}@test.com", username, "hash", firstName, lastName, roleId);
        user.Role = new Role { RoleId = roleId, RoleName = roleName };

        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }
}
