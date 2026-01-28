using Taskboard.Api.Features.Cards;

namespace Taskboard.Api.Tests.Features;

public class CardPositionsTests
{
    [Fact]
    public void Between_EmptyColumn_StartsAtGap() =>
        CardPositions.Between(null, null).Should().Be(CardPositions.Gap);

    [Fact]
    public void Between_AtTop_GoesBelowFirst() =>
        CardPositions.Between(null, 1024).Should().BeLessThan(1024);

    [Fact]
    public void Between_AtBottom_GoesAboveLast() =>
        CardPositions.Between(2048, null).Should().Be(2048 + CardPositions.Gap);

    [Fact]
    public void Between_TwoCards_PicksMiddle() =>
        CardPositions.Between(1024, 2048).Should().Be(1536);

    [Fact]
    public void Between_AdjacentCards_HasNoRoom() =>
        CardPositions.Between(1024, 1025).Should().BeNull();

    [Fact]
    public void Renumber_SpreadsPositionsOut()
    {
        var positions = new List<long> { 5, 6, 7 };

        var result = new long[3];
        CardPositions.Renumber(positions, (p, value) => result[positions.IndexOf(p)] = value);

        result.Should().Equal(1024, 2048, 3072);
    }
}
