using FluentAssertions;
using Moq;
using OmniRoute.Application.Common.DTOs;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Features.Leads.Queries.GetTeamReassignTargets;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.UnitTests.Helpers;

namespace OmniRoute.UnitTests.Features.Leads;

public class GetTeamReassignTargetsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyActiveTeammatesExceptCurrentAssignee()
    {
        var teamId = Guid.NewGuid();
        var assignedUser = CreateUser("assigned", "Assigned", "User", RoleCatalog.Sales, teamId: teamId);
        var eligibleUser = CreateUser("target", "Target", "User", RoleCatalog.Sales, teamId: teamId);
        var inactiveUser = CreateUser("inactive", "Inactive", "User", RoleCatalog.Sales, teamId: teamId, isActive: false);
        var otherTeamUser = CreateUser("other", "Other", "User", RoleCatalog.Sales, teamId: Guid.NewGuid());

        var lead = Lead.Create("L001", "Customer", "0900000000", Channel.Chat, "Need help", Guid.NewGuid());
        lead.AssignToUser(assignedUser.UserId, DateTime.UtcNow.AddHours(1));

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Users).Returns(new[] { assignedUser, eligibleUser, inactiveUser, otherTeamUser }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.Leads).Returns(new[] { lead }.BuildMockDbSet().Object);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.TeamId).Returns(teamId);

        var handler = new GetTeamReassignTargetsQueryHandler(contextMock.Object, currentUserMock.Object);

        var result = await handler.Handle(new GetTeamReassignTargetsQuery(lead.Id, "target"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[]
        {
            new UserPickerOptionDto(
                eligibleUser.UserId,
                "Target User",
                RoleCatalog.Sales,
                RoleCatalog.GetDisplayName(RoleCatalog.Sales))
        });
    }

    private static User CreateUser(
        string username,
        string firstName,
        string lastName,
        string roleName,
        Guid? teamId = null,
        bool isActive = true)
    {
        var roleId = Guid.NewGuid();
        var user = User.Create(Guid.NewGuid(), $"{username}@test.com", username, "hash", firstName, lastName, roleId);
        user.Role = new Role { RoleId = roleId, RoleName = roleName };

        if (teamId.HasValue)
        {
            user.AssignToTeam(teamId.Value);
        }

        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }
}
