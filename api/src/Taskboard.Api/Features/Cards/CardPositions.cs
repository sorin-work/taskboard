namespace Taskboard.Api.Features.Cards;

/// <summary>
/// Cards are ordered by a numeric position with gaps between neighbours, so a
/// move only has to update the card being moved.
/// </summary>
public static class CardPositions
{
    public const long Gap = 1024;

    /// <summary>A position between two neighbours; either side may be missing.</summary>
    public static long Between(long? before, long? after) => (before, after) switch
    {
        (null, null) => Gap,
        (null, long next) => next - Gap,
        (long previous, null) => previous + Gap,
        (long previous, long next) => previous + ((next - previous) / 2),
    };
}
