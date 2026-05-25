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
        Func<int, Duration>? DurationFactory,
        /// <summary>
        /// Non-null for calendar units (year/month/week/day) that have an exact fixed-length
        /// Duration equivalent. Null for variable-length units (month, year) and for time units
        /// that already carry a DurationFactory.
        /// Used by TemporalQuantityParser to convert a calendar-unit literal to a Duration when
        /// the target type is Duration — without a string switch in pipeline code.
        /// </summary>
        Func<int, Duration>? ExactDurationFactory = null)
    {
        public IReadOnlyList<string> Names => [Singular, Plural];
        public bool IsPeriod => PeriodFactory is not null;
        public bool IsDuration => DurationFactory is not null;
    }

    private static readonly TemporalUnitEntry[] Entries =
    [
        new("year",   "years",   true,  Period.FromYears,   null,                                null),
        new("month",  "months",  true,  Period.FromMonths,  null,                                null),
        new("week",   "weeks",   true,  Period.FromWeeks,   null,  n => Duration.FromDays(n * 7)),
        new("day",    "days",    true,  Period.FromDays,    null,  n => Duration.FromDays(n)),
        new("hour",   "hours",   false, null, n => Duration.FromHours(n),   null),
        new("minute", "minutes", false, null, n => Duration.FromMinutes(n), null),
        new("second", "seconds", false, null, n => Duration.FromSeconds(n), null),
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
