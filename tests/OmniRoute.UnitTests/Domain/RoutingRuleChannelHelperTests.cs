using FluentAssertions;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.UnitTests.Domain;

public class RoutingRuleChannelHelperTests
{
    [Fact]
    public void NormalizeConditionChannels_WhenAllChannelsLabelProvided_ReturnsNull()
    {
        var result = RoutingRuleChannelHelper.NormalizeConditionChannels(["all"]);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Walkin", "Walkin")]
    [InlineData("Walk-in", "Walkin")]
    [InlineData("Web", "Webform")]
    [InlineData("Gioi thieu", "Referral")]
    public void NormalizeRequestedChannel_WhenAliasProvided_ReturnsCanonicalValue(string input, string expected)
    {
        var result = RoutingRuleChannelHelper.NormalizeRequestedChannel(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void RuleMatchesLeadChannel_WhenRuleStoresAllChannelsLabel_ReturnsTrue()
    {
        var result = RoutingRuleChannelHelper.RuleMatchesLeadChannel("[\"all\"]", Channel.Walkin);

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
        var wildcardRuleMatch = RoutingRuleChannelHelper.RuleMatchesRequestedChannel("[\"all\"]", "all");
        var specificRuleMatch = RoutingRuleChannelHelper.RuleMatchesRequestedChannel("[\"Walkin\"]", "all");

        wildcardRuleMatch.Should().BeTrue();
        specificRuleMatch.Should().BeFalse();
    }
}
