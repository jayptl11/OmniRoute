using FluentAssertions;
using OmniRoute.Application.Features.MasterData.Queries.GetEnumList;

namespace OmniRoute.UnitTests.Features.MasterData;

public class GetEnumListQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenEnumTypeIsChannel_ReturnsCanonicalValueAndDisplayName()
    {
        var handler = new GetEnumListQueryHandler();

        var result = await handler.Handle(new GetEnumListQuery("Channel"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(x => x.Value == "Walkin" && x.DisplayName == "Walkin");
        result.Value.Should().Contain(x => x.Value == "Referral" && x.DisplayName == "Referral");
    }
}
