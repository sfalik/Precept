using System.Collections.Frozen;
using NodaTime;

namespace Precept.Language;

public static class TemporalUnits
{
    public sealed record TemporalUnitEntry(
        string Singular,
        string Plural,
        bool IsCalendarBased,
        Func<int, Period>? PeriodFactory,
        Func<int, Duration>? DurationFactory)
    {
        public IReadOnlyList<string> Names => [Singular, Plural];
        public bool IsPeriod => PeriodFactory is not null;
        public bool IsDuration => DurationFactory is not null;
    }

    private static readonly TemporalUnitEntry[] Entries =
    [
        new("year", "years", true,  Period.FromYears,   null),
        new("month", "months", true, Period.FromMonths,  null),
        new("week", "weeks", true,  Period.FromWeeks,   null),
        new("day", "days", true,    Period.FromDays,    null),
        new("hour", "hours", false, null, value => Duration.FromHours(value)),
        new("minute", "minutes", false, null, value => Duration.FromMinutes(value)),
        new("second", "seconds", false, null, value => Duration.FromSeconds(value)),
    ];

    public static FrozenDictionary<string, TemporalUnitEntry> All { get; } =
        Entries.ToFrozenDictionary(entry => entry.Singular, StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TemporalUnitEntry> ByName =
        Entries
            .SelectMany(entry => entry.Names.Select(name => new KeyValuePair<string, TemporalUnitEntry>(name, entry)))
            .ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<TemporalUnitEntry> AllEntries => Entries;

    public static bool TryGet(string unitName, out TemporalUnitEntry entry) =>
        ByName.TryGetValue(unitName, out entry!);
}
