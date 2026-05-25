using System.Text.RegularExpressions;
using NodaTime;

namespace Precept.Language;

public static class TemporalQuantityParser
{
    private static readonly Regex PartPattern =
        new(@"^([+-]?\d+)\s+([A-Za-z]+)$", RegexOptions.Compiled);

    public static TemporalParseResult Parse(string rawText, TypeKind? expectedType = null)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP001", "Temporal quantity cannot be empty.", "Use '<integer> <unit>'."));

        var parts = rawText.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP001", "Temporal quantity cannot be empty.", "Use '<integer> <unit>'."));

        var normalizedParts = new List<string>(parts.Length);
        var periodBuilder = new PeriodBuilder();
        var duration = Duration.Zero;
        var sawPeriodUnit = false;
        var sawDurationUnit = false;

        foreach (var part in parts)
        {
            var match = PartPattern.Match(part);
            if (!match.Success)
                return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP002", $"'{part}' is not a valid temporal quantity segment.", "Use '<integer> <unit>'."));

            if (!int.TryParse(match.Groups[1].Value, out var magnitude))
                return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP003", $"'{match.Groups[1].Value}' is not a valid integer magnitude.", "Use whole numbers only."));

            var unitName = match.Groups[2].Value;
            if (!TemporalUnits.TryGet(unitName, out var unit))
                return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP004", $"'{unitName}' is not a recognized temporal unit.", "Use years, months, weeks, days, hours, minutes, or seconds."));

            normalizedParts.Add($"{magnitude} {(Math.Abs(magnitude) == 1 ? unit.Singular : unit.Plural)}");

            if (unit.PeriodFactory is not null)
            {
                // Calendar unit (year, month, week, day)
                sawPeriodUnit = true;
                if (expectedType == TypeKind.Duration)
                {
                    var durationEquiv = CalendarUnitToDuration(unit.Singular, magnitude);
                    if (durationEquiv is null)
                        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP007",
                            $"'{unit.Plural}' have variable length and cannot be used as a duration — months and years are calendar-relative.",
                            "Use days, hours, minutes, or seconds for duration fields."));
                    duration += durationEquiv.Value;
                }
                else
                {
                    AddPeriod(periodBuilder, unit.Singular, magnitude);
                }
            }
            else if (unit.DurationFactory is not null)
            {
                // Time unit (hour, minute, second)
                sawDurationUnit = true;
                if (expectedType == TypeKind.Period)
                    AddPeriodTimeComponent(periodBuilder, unit.Singular, magnitude);
                else
                    duration += unit.DurationFactory(magnitude);
            }
        }

        // Mixed-unit rejection applies only when context is ambiguous (no expectedType)
        if (expectedType is null && sawPeriodUnit && sawDurationUnit)
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP005",
                "Temporal quantities cannot mix calendar units and time units.",
                "Keep years/months/weeks/days separate from hours/minutes/seconds."));

        var canonicalText = string.Join(" + ", normalizedParts);

        if (expectedType == TypeKind.Period)
            return TemporalParseResult.Success(periodBuilder.Build(), canonicalText);

        if (expectedType == TypeKind.Duration)
            return TemporalParseResult.Success(duration, canonicalText);

        if (sawPeriodUnit)
            return TemporalParseResult.Success(periodBuilder.Build(), canonicalText);

        if (sawDurationUnit)
            return TemporalParseResult.Success(duration, canonicalText);

        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP006", "Temporal quantity did not resolve to a supported value.", null));
    }

    private static Duration? CalendarUnitToDuration(string singularUnit, int magnitude) => singularUnit switch
    {
        "day"  => Duration.FromDays(magnitude),
        "week" => Duration.FromDays(magnitude * 7),
        _      => null, // month, year — variable length, no exact Duration equivalent
    };

    private static void AddPeriod(PeriodBuilder builder, string singularUnit, int magnitude)
    {
        switch (singularUnit)
        {
            case "year":  builder.Years  += magnitude; break;
            case "month": builder.Months += magnitude; break;
            case "week":  builder.Weeks  += magnitude; break;
            case "day":   builder.Days   += magnitude; break;
            default: throw new InvalidOperationException($"Unsupported period unit '{singularUnit}'.");
        }
    }

    private static void AddPeriodTimeComponent(PeriodBuilder builder, string singularUnit, int magnitude)
    {
        switch (singularUnit)
        {
            case "hour":   builder.Hours   += magnitude; break;
            case "minute": builder.Minutes += magnitude; break;
            case "second": builder.Seconds += magnitude; break;
            default: throw new InvalidOperationException($"Unsupported time unit for period: '{singularUnit}'.");
        }
    }
}
