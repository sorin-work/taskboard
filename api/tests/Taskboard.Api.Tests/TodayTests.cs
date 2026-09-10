using Taskboard.Api.Common;

namespace Taskboard.Api.Tests;

public class TodayTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly TimeProvider _clock = new FixedClock(new DateTimeOffset(2026, 9, 10, 22, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(null, "2026-09-10")]
    [InlineData("2026-09-11", "2026-09-11")]
    [InlineData("2026-09-09", "2026-09-09")]
    [InlineData("2026-09-13", "2026-09-10")]
    [InlineData("1999-01-01", "2026-09-10")]
    public void Resolve_TrustsTheClientWithinADay(string? client, string expected) =>
        Today.Resolve(client is null ? null : DateOnly.Parse(client, System.Globalization.CultureInfo.InvariantCulture), _clock)
            .Should().Be(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
}
