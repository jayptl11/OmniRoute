using FluentAssertions;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.UnitTests.Domain;

public class RoutingRuleChannelHelperTests
{
    [Fact]
    public void NormalizeConditionChannels_WhenAllChannelsLabelProvided_ReturnsNull()
    {
        var result = RoutingRuleChannelHelper.NormalizeConditionChannels(["Tất cả kênh"]);

        result.Should().BeNull();
    }

    [Fact]
    public void RuleMatchesLeadChannel_WhenRuleStoresAllChannelsLabel_ReturnsTrue()
    {
        var result = RoutingRuleChannelHelper.RuleMatchesLeadChannel("[\"Tất cả kênh\"]", Channel.Walkin);

        result.Should().BeTrue();
    }

    [Fact]
    public void RuleMatchesLeadChannel_WhenRuleStoresWebAlias_MatchesWebformEnum()
    {
        var result = RoutingRuleChannelHelper.RuleMatchesLeadChannel("[\"Web\"]", Channel.Webform);

        result.Should().BeTrue();
    }

    [Fact]
    public void RuleMatchesRequestedChannel_WhenRequestIsAllChannels_OnlyWildcardRulesMatch()
    {
        var wildcardRuleMatch = RoutingRuleChannelHelper.RuleMatchesRequestedChannel("[\"Tất cả kênh\"]", "Tất cả kênh");
        var specificRuleMatch = RoutingRuleChannelHelper.RuleMatchesRequestedChannel("[\"Walkin\"]", "Tất cả kênh");

        wildcardRuleMatch.Should().BeTrue();
        specificRuleMatch.Should().BeFalse();
    }
}
