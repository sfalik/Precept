using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Text;

namespace Precept.Language;

public static class TemporalParser
{
    private static readonly LocalTimePattern HourMinutePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH':'mm");

    private static readonly Regex DateFormatRegex = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex TimeFormatRegex = new(@"^\d{2}:\d{2}(:\d{2})?$", RegexOptions.Compiled);
    private static readonly Regex InstantFormatRegex = new(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", RegexOptions.Compiled);

    public static TemporalParseResult Parse(TemporalLiteralKind kind, string rawText) => kind switch
    {
        TemporalLiteralKind.Date => ParseDate(rawText),
        TemporalLiteralKind.Time => ParseTime(rawText),
        TemporalLiteralKind.DateTime => ParseDateTime(rawText),
        TemporalLiteralKind.Instant => ParseInstant(rawText),
        TemporalLiteralKind.ZonedDateTime => ParseZonedDateTime(rawText),
        TemporalLiteralKind.Timezone => ParseTimezone(rawText),
        TemporalLiteralKind.TemporalQuantity => TemporalQuantityParser.Parse(rawText),
        _ => TemporalParseResult.Failure(new TemporalDiagnostic("TEMP999", $"Unsupported temporal literal kind '{kind}'.", null)),
    };

    private static TemporalParseResult ParseDate(string rawText)
    {
        var result = LocalDatePattern.Iso.Parse(rawText);
        if (result.Success)
            return TemporalParseResult.Success(result.Value, LocalDatePattern.Iso.Format(result.Value));

        // Distinguish format vs semantic: if the input looks like a date (YYYY-MM-DD) but is invalid, it's a semantic error
        var errorKind = DateFormatRegex.IsMatch(rawText)
            ? TypedConstantErrorKind.Semantic
            : TypedConstantErrorKind.Format;

        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP010", "Date must be ISO 8601 YYYY-MM-DD.", null, errorKind));
    }

    private static TemporalParseResult ParseTime(string rawText)
    {
        var extended = LocalTimePattern.ExtendedIso.Parse(rawText);
        if (extended.Success)
            return TemporalParseResult.Success(extended.Value, LocalTimePattern.ExtendedIso.Format(extended.Value));

        var shortResult = HourMinutePattern.Parse(rawText);
        if (shortResult.Success)
            return TemporalParseResult.Success(shortResult.Value, LocalTimePattern.ExtendedIso.Format(shortResult.Value));

        // Distinguish format vs semantic: if the input looks like a time (HH:mm or HH:mm:ss) but is invalid, it's a semantic error
        var errorKind = TimeFormatRegex.IsMatch(rawText)
            ? TypedConstantErrorKind.Semantic
            : TypedConstantErrorKind.Format;

        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP011", "Time must be HH:mm or HH:mm:ss.", null, errorKind));
    }

    private static TemporalParseResult ParseDateTime(string rawText)
    {
        var result = LocalDateTimePattern.ExtendedIso.Parse(rawText);
        if (result.Success)
            return TemporalParseResult.Success(result.Value, LocalDateTimePattern.ExtendedIso.Format(result.Value));

        // For date-time, check if the date portion is semantically invalid
        var tIndex = rawText.IndexOf('T');
        if (tIndex > 0 && DateFormatRegex.IsMatch(rawText[..tIndex]))
        {
            // Format is plausible — likely a semantic error in the date or time component
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP012", "Date-time must be ISO 8601 YYYY-MM-DDThh:mm:ss.", null, TypedConstantErrorKind.Semantic));
        }

        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP012", "Date-time must be ISO 8601 YYYY-MM-DDThh:mm:ss.", null, TypedConstantErrorKind.Format));
    }

    private static TemporalParseResult ParseInstant(string rawText)
    {
        var result = InstantPattern.ExtendedIso.Parse(rawText);
        if (result.Success)
            return TemporalParseResult.Success(result.Value, InstantPattern.ExtendedIso.Format(result.Value));

        // If it looks like an instant (ends with Z and has right structure) but still fails, semantic error
        var errorKind = InstantFormatRegex.IsMatch(rawText)
            ? TypedConstantErrorKind.Semantic
            : TypedConstantErrorKind.Format;

        return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP013", "Instant must be an ISO 8601 UTC timestamp ending with Z.", null, errorKind));
    }

    private static TemporalParseResult ParseZonedDateTime(string rawText)
    {
        var openBracket = rawText.LastIndexOf('[');
        if (openBracket < 0 || !rawText.EndsWith("]", StringComparison.Ordinal))
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP014", "Zoned date-time must end with [Area/Location].", null));

        var dateTimeText = rawText[..openBracket];
        var zoneId = rawText[(openBracket + 1)..^1];

        var dateTimeResult = LocalDateTimePattern.ExtendedIso.Parse(dateTimeText);
        if (!dateTimeResult.Success)
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP015", "Zoned date-time must begin with an ISO 8601 local date-time.", null));

        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneId);
        if (zone is null)
            return TemporalParseResult.Failure(new TemporalDiagnostic("TEMP016", $"'{zoneId}' is not a recognized IANA timezone.", null));

        var zoned = dateTimeResult.Value.InZoneLeniently(zone);
        var canonical = $"{LocalDateTimePattern.ExtendedIso.Format(zoned.LocalDateTime)}[{zone.Id}]";
        return TemporalParseResult.Success(zoned, canonical);
    }

    private static TemporalParseResult ParseTimezone(string rawText)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(rawText);
        return zone is not null
            ? TemporalParseResult.Success(zone, zone.Id)
            : TemporalParseResult.Failure(new TemporalDiagnostic("TEMP017", $"'{rawText}' is not a recognized IANA timezone.", null));
    }
}
