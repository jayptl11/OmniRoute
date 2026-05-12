using FluentAssertions;
using Moq;
using OmniRoute.Application.Features.RoutingRules.Queries.TestRoutingRule;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.UnitTests.Features.RoutingRules;

public class TestRoutingRuleQueryHandlerTests
{
    private readonly Mock<IRoutingRuleRepository> _repositoryMock = new();
    private readonly TestRoutingRuleQueryHandler _handler;

    public TestRoutingRuleQueryHandlerTests()
    {
        _handler = new TestRoutingRuleQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenWildcardChannelRuleMatchesKeyword_ReturnsStoreSupportRule()
    {
        var rule = RoutingRule.Create(
            ruleName: "phân luồng",
            priorityOrder: 10,
            actionGroup: AssignedGroup.StoreSupport,
            conditionChannelJson: "[\"Tất cả kênh\"]",
            conditionKeywordsJson: "[\"đến cửa hàng\"]");

        _repositoryMock
            .Setup(x => x.GetActiveRulesOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);

        var query = new TestRoutingRuleQuery("Khách muốn đến cửa hàng trực tiếp", "Tất cả kênh");

        var result = await _handler.Handle(query, CancellationToken.None);
        var value = result.Value!;

        result.IsSuccess.Should().BeTrue();
        value.Matched.Should().BeTrue();
        value.MatchedRuleName.Should().Be("phân luồng");
        value.ResultGroup.Should().Be("StoreSupport");
    }

    [Fact]
    public async Task Handle_WhenNoRuleMatches_ReturnsStoreSupportFallback()
    {
        _repositoryMock
            .Setup(x => x.GetActiveRulesOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new TestRoutingRuleQuery("Nội dung không khớp", "Hotline");

        var result = await _handler.Handle(query, CancellationToken.None);
        var value = result.Value!;

        result.IsSuccess.Should().BeTrue();
        value.Matched.Should().BeFalse();
        value.ResultGroup.Should().Be("StoreSupport");
    }
}
