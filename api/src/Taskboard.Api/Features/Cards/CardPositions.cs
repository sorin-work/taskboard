namespace Taskboard.Api.Features.Cards;

/// <summary>
/// Cards are ordered by a numeric position with gaps between neighbours, so a
/// move usually only has to update the card being moved. When two neighbours
/// end up next to each other there is no room left between them, and the
/// column gets renumbered.
/// </summary>
public static class CardPositions
{
    public const long Gap = 1024;

    /// <summary>A position between two neighbours (either may be missing), or null when they are adjacent.</summary>
    public static long? Between(long? before, long? after) => (before, after) switch
    {
        (null, null) => Gap,
        (null, long next) => next - Gap,
        (long previous, null) => previous + Gap,
        (long previous, long next) when next - previous > 1 => previous + ((next - previous) / 2),
        _ => null,
    };

    /// <summary>Spreads positions out evenly again, keeping the current order.</summary>
    public static void Renumber<T>(IList<T> ordered, Action<T, long> setPosition)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            setPosition(ordered[i], (i + 1) * Gap);
        }
    }
}
