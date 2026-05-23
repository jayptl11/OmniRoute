using FluentAssertions;
using Moq;
using OmniRoute.Application.Features.Stores.Queries.GetStoresCapacity;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.UnitTests.Features.Stores;

public class GetStoresCapacityQueryHandlerTests
{
    [Fact]
    public async Task Handle_PassesSearchTextToRepository_AndMapsCapacityFields()
    {
        var store = Store.Create("ST01", "Store Alpha", 10, "123 Street", "HCM");
        store.Activate();

        var repositoryMock = new Mock<IStoreRepository>();
        repositoryMock
            .Setup(x => x.GetStoresWithActiveLeadCountAsync("alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync([(store, 3)]);

        var handler = new GetStoresCapacityQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetStoresCapacityQuery("alpha"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].StoreCode.Should().Be("ST01");
        result.Value[0].StoreName.Should().Be("Store Alpha");
        result.Value[0].ActiveLeads.Should().Be(3);
        result.Value[0].AvailableSlots.Should().Be(7);

        repositoryMock.Verify(x => x.GetStoresWithActiveLeadCountAsync("alpha", It.IsAny<CancellationToken>()), Times.Once);
    }
}
