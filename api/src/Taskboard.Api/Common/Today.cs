namespace Taskboard.Api.Common;

/// <summary>
/// "Today" for due dates. Browsers send their own date (?today=YYYY-MM-DD),
/// since UTC's today is already tomorrow in the evening in Asia and still
/// yesterday in the morning in the Americas.
/// </summary>
public static class Today
{
    public static DateOnly Resolve(DateOnly? fromClient, TimeProvider clock)
    {
        var utcToday = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        // Every time zone is within a day of UTC; anything further is a bad clock or a typo.
        return fromClient is { } day && Math.Abs(day.DayNumber - utcToday.DayNumber) <= 1 ? day : utcToday;
    }
}
