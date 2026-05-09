using System.Text.RegularExpressions;
using NodaTime;

namespace Precept.Language;

public static class TemporalQuantityParser
{
    private static readonly Regex PartPattern =
        new(@"^([+-]?\d+)\s+([A-Za-z]+)$", RegexOptions.Compiled);

    public static TemporalParseResult Parse(string rawText)
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
                sawPeriodUnit = true;
                AddPeriod(periodBuilder, unit.Singular, magnitude);
            }
            else if (unit.DurationFactory is not null)
            {
                sawDurationUnit = true;
                duration += unit.DurationFactory(magnitude);
            }
        }

        if (sawPeriodUnit && sawDurationUnit)
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP005", "Temporal quantities cannot mix calendar units and time units.", "Keep years/months/weeks/days separate from hours/minutes/seconds."));

        var canonicalText = string.Join(" + ", normalizedParts);
        if (sawPeriodUnit)
            return TemporalParseResult.Success(periodBuilder.Build(), canonicalText);

        if (sawDurationUnit)
            return TemporalParseResult.Success(duration, canonicalText);

        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP006", "Temporal quantity did not resolve to a supported value.", null));
    }

    private static void AddPeriod(PeriodBuilder builder, string singularUnit, int magnitude)
    {
        switch (singularUnit)
        {
            case "year":
                builder.Years += magnitude;
                break;
            case "month":
                builder.Months += magnitude;
                break;
            case "week":
                builder.Weeks += magnitude;
                break;
            case "day":
                builder.Days += magnitude;
                break;
            default:
                throw new InvalidOperationException($"Unsupported period unit '{singularUnit}'.");
        }
    }
}
