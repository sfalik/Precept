using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    // ════════════════════════════════════════════════════════════════════════
    //  Expression resolution — Slice 3: Functions, Accessors, Interpolated Strings
    // ════════════════════════════════════════════════════════════════════════

    private static bool TryMatchCompoundUnitCancellation(
        TypedExpression standaloneQuantity,
        TypedExpression compoundQuantity,
        out DeclaredQualifierMeta.Unit resultUnit)
    {
        if (!TryGetQuantityDimensionName(standaloneQuantity, out var standaloneDimension)
            || !TryGetCompoundUnit(compoundQuantity, out var compoundUnit)
            || !TrySplitCompoundUnit(compoundUnit.UnitCode, out var numeratorUnit, out var denominatorUnit)
            || !TryDeriveUnitDimensionName(denominatorUnit, out var denominatorDimension)
            || !string.Equals(standaloneDimension, denominatorDimension, StringComparison.OrdinalIgnoreCase)
            || !TryDeriveUnitDimensionName(numeratorUnit, out var numeratorDimension))
        {
            resultUnit = null!;
            return false;
        }

        resultUnit = new DeclaredQualifierMeta.Unit(
            numeratorUnit,
            numeratorDimension,
            QualifierOrigin.Derived,
            compoundUnit.Preposition,
            compoundUnit.ProofSatisfactions);
        return true;
    }

    private static bool TryGetQuantityDimensionName(TypedExpression value, out string dimensionName)
    {
        var resolution = ResolveAssignmentQualifierAxis(value, QualifierAxis.Dimension);
        if (resolution.Kind == QualifierResolutionKind.Resolved
            && resolution.Qualifier is not null
            && TryGetQualifierText(resolution.Qualifier, QualifierAxis.Dimension, out var resolvedDimension)
            && !string.IsNullOrWhiteSpace(resolvedDimension))
        {
            dimensionName = resolvedDimension;
            return true;
        }

        dimensionName = string.Empty;
        return false;
    }

    private static bool TryGetCompoundUnit(TypedExpression value, out DeclaredQualifierMeta.Unit compoundUnit)
    {
        compoundUnit = null!;

        var unitResolution = ResolveAssignmentQualifierAxis(value, QualifierAxis.Unit);
        if (unitResolution.Kind == QualifierResolutionKind.Resolved
            && unitResolution.Qualifier is DeclaredQualifierMeta.Unit unit
            && unit.UnitCode.IndexOf('/') > 0)
        {
            compoundUnit = unit;
            return true;
        }

        var dimensionResolution = ResolveAssignmentQualifierAxis(value, QualifierAxis.Dimension);
        if (dimensionResolution.Kind == QualifierResolutionKind.Resolved
            && dimensionResolution.Qualifier is not null
            && TryGetQualifierText(dimensionResolution.Qualifier, QualifierAxis.Dimension, out var dimensionName)
            && dimensionName.IndexOf('/') > 0)
        {
            compoundUnit = new DeclaredQualifierMeta.Unit(dimensionName, dimensionName, QualifierOrigin.Derived);
            return true;
        }

        return false;
    }

    private static bool TrySplitCompoundUnit(string unitCode, out string numeratorUnit, out string denominatorUnit) =>
        QualifierUnitHelpers.TrySplitCompoundUnit(unitCode, out numeratorUnit, out denominatorUnit);

    private static bool TryDeriveUnitDimensionName(string unitCode, out string dimensionName) =>
        QualifierUnitHelpers.TryDeriveUnitDimensionName(unitCode, out dimensionName);

    // ════════════════════════════════════════════════════════════════════════
    //  Interpolated typed constant resolution (Slice 2)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Types that do not support interpolation.</summary>
    private static readonly FrozenSet<TypeKind> InterpolationUnsupportedTypes = new[]
    {
        TypeKind.Date, TypeKind.Time, TypeKind.Instant,
        TypeKind.DateTime, TypeKind.ZonedDateTime, TypeKind.Timezone,
    }.ToFrozenSet();

    private static readonly IReadOnlyDictionary<TypeKind, string> InterpolationUnsupportedGuidance =
        new Dictionary<TypeKind, string>
        {
            [TypeKind.Date]          = "Date values like '2026-04-15' must be written as complete literals. To compute dates dynamically, use arithmetic: StartDate + '{n} days'.",
            [TypeKind.Time]          = "Time values like '14:30:00' must be written as complete literals. To compute times dynamically, use arithmetic: StartTime + '{n} hours'.",
            [TypeKind.Instant]       = "Instant values must be written as complete literals.",
            [TypeKind.DateTime]      = "DateTime values must be written as complete literals.",
            [TypeKind.ZonedDateTime] = "ZonedDateTime values must be written as complete literals.",
            [TypeKind.Timezone]      = "Timezone values like 'America/New_York' must be written as complete literals.",
        };

    /// <summary>
    /// Classifies text content for grammar matching.
    /// </summary>
    private enum TextClass { Numeric = 1, CurrencyCode, UnitName, Empty, Other }

    private static bool IsNumericLiteral(string s) =>
        s.Length > 0 && decimal.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture, out _);

    private static bool IsIntegerLiteral(string s) =>
        s.Length > 0 && long.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out _);

    private static bool IsCurrencyCode(string s) =>
        s.Length > 0 && CurrencyCatalog.All.ContainsKey(s.ToUpperInvariant());

    private static bool IsUnitName(string s) =>
        s.Length > 0 && (UcumCatalog.IsValid(s) || TemporalUnits.TryGet(s, out _));

    /// <summary>
    /// A segment-aware form pattern. For N holes, the pattern describes:
    /// text₀, slot₁, text₁, slot₂, ..., textₙ where each textᵢ is a classifier
    /// for the text between holes. The slots array has N entries, the texts array has N+1 entries.
    /// </summary>
    private sealed record SegmentForm(TextMatch[] TextChecks, InterpolationSlotKind[] Slots);

    /// <summary>Checker for text content between holes.</summary>
    private delegate bool TextMatch(string text);

    // ── Text matchers ──────────────────────────────────────────────────────

    private static bool MatchEmpty(string text) => text.Length == 0 || string.IsNullOrWhiteSpace(text);
    private static bool MatchSpaceCurrency(string text) => text.StartsWith(" ", StringComparison.Ordinal) && IsCurrencyCode(text.TrimStart());
    private static bool MatchSpaceUnit(string text) => text.StartsWith(" ", StringComparison.Ordinal) && IsUnitName(text.TrimStart());
    private static bool MatchNumericSpace(string text) => text.EndsWith(" ", StringComparison.Ordinal) && IsNumericLiteral(text.TrimEnd());
    private static bool MatchIntegerSpace(string text) => text.EndsWith(" ", StringComparison.Ordinal) && IsIntegerLiteral(text.TrimEnd());
    private static bool MatchSpaceCurrencySlash(string text)
    {
        if (!text.StartsWith(" ", StringComparison.Ordinal)) return false;
        var rest = text.TrimStart();
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && rest.Length == slashIdx + 1;
    }
    private static bool MatchSpaceCurrencySlashUnit(string text)
    {
        if (!text.StartsWith(" ", StringComparison.Ordinal)) return false;
        var rest = text.TrimStart();
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && IsUnitName(rest[(slashIdx + 1)..]);
    }
    private static bool MatchSlashCurrency(string text) => text.StartsWith("/", StringComparison.Ordinal) && IsCurrencyCode(text[1..]);
    private static bool MatchSlashUnit(string text) => text.StartsWith("/", StringComparison.Ordinal) && IsUnitName(text[1..]);
    private static bool MatchUnitSlash(string text) => text.EndsWith("/", StringComparison.Ordinal) && IsUnitName(text[..^1]);
    private static bool MatchSlash(string text) => text == "/";
    private static bool MatchNumericSpaceCurrencySlash(string text)
    {
        if (!text.EndsWith("/", StringComparison.Ordinal)) return false;
        var content = text[..^1];
        var spaceIdx = content.IndexOf(' ');
        return spaceIdx > 0 && IsNumericLiteral(content[..spaceIdx]) && IsCurrencyCode(content[(spaceIdx + 1)..]);
    }
    private static bool MatchNumericSpaceCurrencySlashUnit(string text)
    {
        var spaceIdx = text.IndexOf(' ');
        if (spaceIdx <= 0) return false;
        if (!IsNumericLiteral(text[..spaceIdx])) return false;
        var rest = text[(spaceIdx + 1)..];
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && IsUnitName(rest[(slashIdx + 1)..]);
    }
    private static bool MatchNumericSpaceUnitSlash(string text)
    {
        if (!text.EndsWith("/", StringComparison.Ordinal)) return false;
        var content = text[..^1];
        var spaceIdx = content.IndexOf(' ');
        return spaceIdx > 0 && IsNumericLiteral(content[..spaceIdx]) && IsUnitName(content[(spaceIdx + 1)..]);
    }

    // ── Per-type valid form tables (segment-aware) ────────────────────────

    private static readonly SegmentForm[] MoneyForms =
    [
        // M1: H[whole-value]  — text: ("", "")
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        // M2: H[magnitude] " USD"  — text: ("", " USD")
        new([MatchEmpty, MatchSpaceCurrency], [InterpolationSlotKind.Magnitude]),
        // M3: "100 " H[currency]  — text: ("100 ", "")
        new([MatchNumericSpace, MatchEmpty], [InterpolationSlotKind.Currency]),
        // M4: H[magnitude] " " H[currency]  — text: ("", " ", "")
        new([MatchEmpty, (string s) => s == " ", MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Currency]),
    ];

    private static readonly SegmentForm[] QuantityForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        new([MatchEmpty, MatchSpaceUnit], [InterpolationSlotKind.Magnitude]),
        new([MatchNumericSpace, MatchEmpty], [InterpolationSlotKind.Unit]),
        new([MatchEmpty, (string s) => s == " ", MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Unit]),
        new([MatchEmpty, (string s) => s == " ", MatchSlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.NumeratorUnit, InterpolationSlotKind.DenominatorUnit]),
        new([MatchNumericSpace, MatchSlash, MatchEmpty], [InterpolationSlotKind.NumeratorUnit, InterpolationSlotKind.DenominatorUnit]),
        new([MatchNumericSpace, MatchSlashUnit], [InterpolationSlotKind.NumeratorUnit]),
        new([MatchNumericSpaceUnitSlash, MatchEmpty], [InterpolationSlotKind.DenominatorUnit]),
    ];

    private static readonly SegmentForm[] PriceForms =
    [
        // P1: H[whole-value]
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        // P2: H[magnitude] " USD/each"
        new([MatchEmpty, MatchSpaceCurrencySlashUnit], [InterpolationSlotKind.Magnitude]),
        // P3: "4.17 " H[currency] "/each"
        new([MatchNumericSpace, MatchSlashUnit], [InterpolationSlotKind.Currency]),
        // P4: "4.17 USD/" H[unit]
        new([MatchNumericSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Unit]),
        // P5: "4.17 " H[currency] "/" H[unit]
        new([MatchNumericSpace, MatchSlash, MatchEmpty], [InterpolationSlotKind.Currency, InterpolationSlotKind.Unit]),
        // P6: H[magnitude] " " H[currency] "/each"
        new([MatchEmpty, (string s) => s == " ", MatchSlashUnit], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Currency]),
        // P7: H[magnitude] " USD/" H[unit]
        new([MatchEmpty, MatchSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Unit]),
        // P8: H[magnitude] " " H[currency] "/" H[unit]
        new([MatchEmpty, (string s) => s == " ", MatchSlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Currency, InterpolationSlotKind.Unit]),
    ];

    private static readonly SegmentForm[] ExchangeRateForms =
    [
        // X1: H[whole-value]
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        // X2: H[magnitude] " USD/EUR"
        new([MatchEmpty, MatchSpaceCurrencySlashCurrency], [InterpolationSlotKind.Magnitude]),
        // X3: "1.08 " H[from-currency] "/EUR"
        new([MatchNumericSpace, MatchSlashCurrency], [InterpolationSlotKind.FromCurrency]),
        // X4: "1.08 USD/" H[to-currency]
        new([MatchNumericSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.ToCurrency]),
        // X5: "1.08 " H[from-currency] "/" H[to-currency]
        new([MatchNumericSpace, MatchSlash, MatchEmpty], [InterpolationSlotKind.FromCurrency, InterpolationSlotKind.ToCurrency]),
        // X6: H[magnitude] " " H[from-currency] "/EUR"
        new([MatchEmpty, (string s) => s == " ", MatchSlashCurrency], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.FromCurrency]),
        // X7: H[magnitude] " USD/" H[to-currency]
        new([MatchEmpty, MatchSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.ToCurrency]),
        // X8: H[magnitude] " " H[from-currency] "/" H[to-currency]
        new([MatchEmpty, (string s) => s == " ", MatchSlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.FromCurrency, InterpolationSlotKind.ToCurrency]),
    ];

    private static bool MatchSpaceCurrencySlashCurrency(string text)
    {
        if (!text.StartsWith(" ", StringComparison.Ordinal)) return false;
        var rest = text.TrimStart();
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && IsCurrencyCode(rest[(slashIdx + 1)..]);
    }

    private static readonly SegmentForm[] SingleComponentForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
    ];

    private static readonly SegmentForm[] UnitOfMeasureForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        new([MatchEmpty, MatchSlash, MatchEmpty], [InterpolationSlotKind.NumeratorUnit, InterpolationSlotKind.DenominatorUnit]),
        new([MatchEmpty, MatchSlashUnit], [InterpolationSlotKind.NumeratorUnit]),
        new([MatchUnitSlash, MatchEmpty], [InterpolationSlotKind.DenominatorUnit]),
    ];

    private static readonly SegmentForm[] TemporalSingleForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        new([MatchEmpty, MatchSpaceUnit], [InterpolationSlotKind.Magnitude]),
        new([MatchIntegerSpace, MatchEmpty], [InterpolationSlotKind.Unit]),
        new([MatchEmpty, (string s) => s == " ", MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Unit]),
    ];

    /// <summary>
    /// Gets the applicable form patterns for a target type.
    /// Returns null if the type doesn't support interpolation at all.
    /// </summary>
    private static SegmentForm[]? GetFormsForType(TypeKind type) => type switch
    {
        TypeKind.Money         => MoneyForms,
        TypeKind.Quantity      => QuantityForms,
        TypeKind.Price         => PriceForms,
        TypeKind.ExchangeRate  => ExchangeRateForms,
        TypeKind.Currency      => SingleComponentForms,
        TypeKind.UnitOfMeasure => UnitOfMeasureForms,
        TypeKind.Dimension     => SingleComponentForms,
        TypeKind.Duration      => TemporalSingleForms,
        TypeKind.Period        => TemporalSingleForms,
        _ => null,
    };

    /// <summary>
    /// Returns the set of types compatible with a given slot for a given target type.
    /// </summary>
    private static bool IsSlotCompatible(InterpolationSlotKind slot, TypeKind holeType, TypeKind targetType, bool isTemporal)
    {
        if (holeType == TypeKind.String) return false;
        if (holeType == TypeKind.Error) return true; // suppress cascading

        return slot switch
        {
            InterpolationSlotKind.Magnitude when isTemporal
                => holeType == TypeKind.Integer,
            InterpolationSlotKind.Magnitude
                => holeType is TypeKind.Integer or TypeKind.Decimal or TypeKind.Number,
            InterpolationSlotKind.Currency or InterpolationSlotKind.FromCurrency or InterpolationSlotKind.ToCurrency
                => holeType == TypeKind.Currency,
            InterpolationSlotKind.Unit or InterpolationSlotKind.NumeratorUnit or InterpolationSlotKind.DenominatorUnit
                => holeType == TypeKind.UnitOfMeasure,
            InterpolationSlotKind.WholeValue
                => holeType == targetType,
            _ => false,
        };
    }

    private static string SlotCompatibleTypesDescription(InterpolationSlotKind slot, bool isTemporal) => slot switch
    {
        InterpolationSlotKind.Magnitude when isTemporal => "integer",
        InterpolationSlotKind.Magnitude                 => "integer, decimal, or number",
        InterpolationSlotKind.Currency or InterpolationSlotKind.FromCurrency or InterpolationSlotKind.ToCurrency
                                                        => "currency",
        InterpolationSlotKind.Unit or InterpolationSlotKind.NumeratorUnit or InterpolationSlotKind.DenominatorUnit
                                                        => "unitofmeasure",
        InterpolationSlotKind.WholeValue                => "the target type",
        _ => "unknown",
    };

    /// <summary>
    /// Try to match a segment sequence against a segment-aware form pattern.
    /// The segment sequence is 2N+1 elements for N holes (alternating text, hole, text, ...).
    /// The form has N+1 text checks and N slot assignments.
    /// Returns slot assignments on success, null on failure.
    /// </summary>
    private static List<(int HoleIndex, InterpolationSlotKind Slot)>? TryMatchForm(
        SegmentForm form,
        ImmutableArray<InterpolationSegment> segments)
    {
        int expectedHoles = form.Slots.Length;
        int expectedSegments = 2 * expectedHoles + 1;
        if (segments.Length != expectedSegments) return null;

        // Validate text segments
        for (int i = 0; i < form.TextChecks.Length; i++)
        {
            int segIdx = i * 2;
            if (i > expectedHoles) segIdx = segments.Length - 1; // shouldn't happen
            else segIdx = i <= expectedHoles ? i * 2 : segments.Length - 1;

            // For N holes: text segments are at indices 0, 2, 4, ..., 2N
            // TextChecks[0] → segment[0], TextChecks[1] → segment[2], ..., TextChecks[N] → segment[2N]
            int textSegIdx = i * 2;
            if (textSegIdx >= segments.Length) return null;
            if (segments[textSegIdx] is not TextSegment text) return null;
            if (!form.TextChecks[i](text.Text)) return null;
        }

        // Validate hole segments exist and collect slots
        var slots = new List<(int, InterpolationSlotKind)>(expectedHoles);
        for (int h = 0; h < expectedHoles; h++)
        {
            int holeSegIdx = h * 2 + 1;
            if (segments[holeSegIdx] is not HoleSegment) return null;
            slots.Add((h, form.Slots[h]));
        }

        return slots;
    }

    /// <summary>
    /// Attempts to match temporal compound forms (D5–D7 / Pe5–Pe7) for duration/period.
    /// Pattern: '{h} hours + {m} minutes' → TextSegment(""), HoleSegment(h), TextSegment(" hours + "), HoleSegment(m), TextSegment(" minutes")
    /// </summary>
    private static List<(int HoleIndex, InterpolationSlotKind Slot)>? TryMatchTemporalCompound(
        ImmutableArray<InterpolationSegment> segments)
    {
        // Must have at least 2 holes → 5 segments
        if (segments.Length < 5) return null;

        // Must have odd count (2N+1)
        if (segments.Length % 2 == 0) return null;

        int holeCount = segments.Length / 2;
        var slots = new List<(int, InterpolationSlotKind)>();

        for (int i = 0; i < segments.Length; i++)
        {
            if (i % 2 == 0)
            {
                // Text segment
                if (segments[i] is not TextSegment text) return null;
                string t = text.Text;

                if (i == 0)
                {
                    // First text: empty (hole follows) or integer followed by space
                    if (t.Length == 0 || string.IsNullOrWhiteSpace(t))
                    {
                        // Next is a hole
                    }
                    else if (t.EndsWith(" ", StringComparison.Ordinal) && IsIntegerLiteral(t.TrimEnd()))
                    {
                        // Static first component — no hole for this
                    }
                    else return null;
                }
                else if (i == segments.Length - 1)
                {
                    // Last text: must end with a temporal unit name
                    var trimmed = t.Trim();
                    if (trimmed.Length == 0 || !(TemporalUnits.TryGet(trimmed, out _) || UcumCatalog.IsValid(trimmed)))
                        return null;
                }
                else
                {
                    // Middle text between holes: must contain " <unit> + " pattern
                    if (!t.Contains(" + ", StringComparison.Ordinal)) return null;
                    // The text is like " hours + " — unit before the +, space after
                    var plusIdx = t.IndexOf(" + ", StringComparison.Ordinal);
                    var unitPart = t[..plusIdx].Trim();
                    if (unitPart.Length == 0 || !(TemporalUnits.TryGet(unitPart, out _) || UcumCatalog.IsValid(unitPart)))
                        return null;
                }
            }
            else
            {
                // Hole segment
                if (segments[i] is not HoleSegment) return null;
                slots.Add((i / 2, InterpolationSlotKind.Magnitude));
            }
        }

        // Check first text: if it was numeric (static), need to remove
        // the first hole index mapping since there's no hole for that component
        if (segments[0] is TextSegment ft && ft.Text.Length > 0 && !string.IsNullOrWhiteSpace(ft.Text))
        {
            // First component is static — all holes are for subsequent components
            // Already handled correctly since we only add holes at odd indices
        }

        return slots.Count >= 1 ? slots : null;
    }

    /// <summary>
    /// Full type-grammar matching resolution for interpolated typed constants.
    /// Implements the algorithm from the plan §Type-Grammar Matching Algorithm.
    /// </summary>
    private static TypedExpression ResolveInterpolatedTypedConstant(
        InterpolatedTypedConstantExpression expr,
        CheckContext ctx,
        TypeKind? expectedType,
        ImmutableArray<DeclaredQualifierMeta>? qualifiers)
    {
        // Step 1–2: Target type from context
        if (expectedType is not { } targetType || targetType == TypeKind.Error)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UnresolvedTypedConstant, expr.Span, "(interpolated)"));
            return new TypedErrorExpression(expr.Span);
        }

        // Step 3: Unsupported types
        if (InterpolationUnsupportedTypes.Contains(targetType))
        {
            var guidance = InterpolationUnsupportedGuidance.GetValueOrDefault(targetType, "");
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InterpolationNotSupportedForType, expr.Span,
                    Types.GetMeta(targetType).DisplayName, guidance));
            return new TypedErrorExpression(expr.Span);
        }

        // Step 4: Extract segments
        var segments = expr.Segments;

        // Step 5–6: Match against form grammars
        var forms = GetFormsForType(targetType);
        if (forms is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidInterpolatedTypedConstantForm, expr.Span,
                    Types.GetMeta(targetType).DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        List<(int HoleIndex, InterpolationSlotKind Slot)>? matchedSlots = null;

        foreach (var form in forms)
        {
            matchedSlots = TryMatchForm(form, segments);
            if (matchedSlots is not null) break;
        }

        // For duration/period, also try compound forms if single-component didn't match
        if (matchedSlots is null && targetType is TypeKind.Duration or TypeKind.Period)
        {
            matchedSlots = TryMatchTemporalCompound(segments);
        }

        // Step 7: No match → structural error
        if (matchedSlots is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidInterpolatedTypedConstantForm, expr.Span,
                    Types.GetMeta(targetType).DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        // Step 8: Resolve each hole expression and validate slot compatibility
        bool isTemporal = targetType is TypeKind.Duration or TypeKind.Period;
        var holes = segments.OfType<HoleSegment>().ToArray();
        var typedSlots = ImmutableArray.CreateBuilder<TypedInterpolationSlot>(matchedSlots.Count);
        bool hasError = false;

        foreach (var (holeIndex, slotKind) in matchedSlots)
        {
            if (holeIndex >= holes.Length)
            {
                hasError = true;
                continue;
            }

            var hole = holes[holeIndex];

            // Determine advisory expected type for the hole
            TypeKind? slotExpectedType = slotKind switch
            {
                InterpolationSlotKind.Magnitude when isTemporal => TypeKind.Integer,
                InterpolationSlotKind.Magnitude                 => TypeKind.Decimal,
                InterpolationSlotKind.Currency                  => TypeKind.Currency,
                InterpolationSlotKind.FromCurrency              => TypeKind.Currency,
                InterpolationSlotKind.ToCurrency                => TypeKind.Currency,
                InterpolationSlotKind.Unit                      => TypeKind.UnitOfMeasure,
                InterpolationSlotKind.NumeratorUnit             => TypeKind.UnitOfMeasure,
                InterpolationSlotKind.DenominatorUnit           => TypeKind.UnitOfMeasure,
                InterpolationSlotKind.WholeValue                => targetType,
                _ => null,
            };

            var resolved = Resolve(hole.Expression, ctx, slotExpectedType);

            // Check slot compatibility
            if (resolved is not TypedErrorExpression && !IsSlotCompatible(slotKind, resolved.ResultType, targetType, isTemporal))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch, hole.Span,
                        Types.GetMeta(resolved.ResultType).DisplayName,
                        slotKind.ToString().ToLowerInvariant(),
                        SlotCompatibleTypesDescription(slotKind, isTemporal)));
                hasError = true;
            }

            typedSlots.Add(new TypedInterpolationSlot(resolved, slotKind));
        }

        if (hasError)
            return new TypedErrorExpression(expr.Span);

        var typedSlotsArray = typedSlots.ToImmutable();

        // Step 9: Dimension-unit consistency for unit-slot holes
        foreach (var slot in typedSlotsArray)
        {
            if (slot.SlotKind != InterpolationSlotKind.Unit) continue;
            if (slot.Expression.ResultType != TypeKind.UnitOfMeasure) continue;

            ValidateUnitSlotDimensionConsistency(slot.Expression, qualifiers, expr.Span, ctx);
        }

        return new InterpolatedTypedConstant(
            typedSlotsArray,
            targetType,
            expr.Span,
            TryExtractStaticMagnitude(segments),
            ResolveStaticQualifier(segments, typedSlotsArray, targetType),
            string.Concat(segments.OfType<TextSegment>().Select(segment => segment.Text)));
    }

    private static decimal? TryExtractStaticMagnitude(ImmutableArray<InterpolationSegment> segments)
    {
        foreach (var segment in segments)
        {
            switch (segment)
            {
                case TextSegment { Text: var text } when string.IsNullOrWhiteSpace(text):
                    continue;

                case HoleSegment:
                    return null;

                case TextSegment { Text: var text }:
                {
                    var trimmed = text.TrimStart();
                    var end = trimmed.IndexOf(' ');
                    var token = end >= 0 ? trimmed[..end] : trimmed;
                    return decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                        ? value
                        : null;
                }
            }
        }

        return null;
    }

    private static StaticInterpolatedQualifier? ResolveStaticQualifier(
        ImmutableArray<InterpolationSegment> segments,
        ImmutableArray<TypedInterpolationSlot> typedSlots,
        TypeKind targetType)
    {
        if (typedSlots.Any(slot => slot.SlotKind == InterpolationSlotKind.WholeValue))
            return null;

        var hasCurrencySlot = typedSlots.Any(slot => slot.SlotKind == InterpolationSlotKind.Currency);
        var hasUnitSlot = typedSlots.Any(slot => slot.SlotKind is InterpolationSlotKind.Unit or InterpolationSlotKind.NumeratorUnit or InterpolationSlotKind.DenominatorUnit);
        var hasFromSlot = typedSlots.Any(slot => slot.SlotKind == InterpolationSlotKind.FromCurrency);
        var hasToSlot = typedSlots.Any(slot => slot.SlotKind == InterpolationSlotKind.ToCurrency);
        var staticText = string.Concat(segments.OfType<TextSegment>().Select(segment => segment.Text)).Trim();

        return targetType switch
        {
            TypeKind.Money when !hasCurrencySlot && TryExtractCurrency(staticText, out var currencyCode) =>
                new StaticCurrencyQualifier(currencyCode),

            TypeKind.Quantity when !hasUnitSlot && TryExtractUnit(staticText, out var unit) =>
                new StaticUnitQualifier(unit),

            TypeKind.Price when !hasCurrencySlot && !hasUnitSlot
                             && TryExtractCurrencyAndUnit(staticText, out var priceCurrency, out var priceUnit) =>
                new StaticCurrencyAndUnitQualifier(priceCurrency, priceUnit),

            TypeKind.Price when !hasCurrencySlot && TryExtractCurrencyAndUnit(staticText, out var staticCurrency, out _) =>
                new StaticCurrencyQualifier(staticCurrency),

            TypeKind.Price when !hasUnitSlot && TryExtractCurrencyAndUnit(staticText, out _, out var staticUnit) =>
                new StaticUnitQualifier(staticUnit),

            TypeKind.ExchangeRate when !hasFromSlot && !hasToSlot
                                    && TryExtractFromToCurrencies(staticText, out var fromCode, out var toCode) =>
                new StaticFromToCurrenciesQualifier(fromCode, toCode),

            _ => null,
        };
    }

    private static bool TryExtractCurrency(string text, out string currencyCode)
    {
        currencyCode = string.Empty;
        var token = text.Trim();
        if (token.Contains('/'))
            token = token[..token.IndexOf('/')];

        if (!IsCurrencyCode(token))
            return false;

        currencyCode = token.ToUpperInvariant();
        return true;
    }

    private static bool TryExtractUnit(string text, out UcumParsedUnit unit)
    {
        unit = null!;
        var token = text.Trim();
        if (token.Length == 0)
            return false;

        var result = UcumParser.Parse(token);
        if (!result.IsValid || result.Unit is null)
            return false;

        unit = result.Unit;
        return true;
    }

    private static bool TryExtractCurrencyAndUnit(string text, out string currencyCode, out UcumParsedUnit unit)
    {
        currencyCode = string.Empty;
        unit = null!;

        var trimmed = text.Trim();
        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == trimmed.Length - 1)
            return false;

        var currency = trimmed[..slashIndex].Trim();
        var unitCode = trimmed[(slashIndex + 1)..].Trim();
        if (!IsCurrencyCode(currency))
            return false;

        var parsed = UcumParser.Parse(unitCode);
        if (!parsed.IsValid || parsed.Unit is null)
            return false;

        currencyCode = currency.ToUpperInvariant();
        unit = parsed.Unit;
        return true;
    }

    private static bool TryExtractInterpolatedPriceCurrency(string? staticText, out string currencyCode)
    {
        currencyCode = string.Empty;

        if (string.IsNullOrWhiteSpace(staticText))
            return false;

        var trimmed = staticText.Trim();
        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex <= 0)
            return false;

        var beforeSlash = trimmed[..slashIndex].Trim();
        if (beforeSlash.Length == 0)
            return false;

        var parts = beforeSlash.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        var currency = parts[^1];
        if (!IsCurrencyCode(currency))
            return false;

        currencyCode = currency.ToUpperInvariant();
        return true;
    }

    private static bool TryExtractFromToCurrencies(string text, out string fromCode, out string toCode)
    {
        fromCode = string.Empty;
        toCode = string.Empty;

        var trimmed = text.Trim();
        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == trimmed.Length - 1)
            return false;

        var from = trimmed[..slashIndex].Trim();
        var to = trimmed[(slashIndex + 1)..].Trim();

        if (!IsCurrencyCode(from) || !IsCurrencyCode(to))
            return false;

        fromCode = from.ToUpperInvariant();
        toCode = to.ToUpperInvariant();
        return true;
    }

    /// <summary>
    /// Dimension-unit consistency check for unit-slot holes in interpolated typed constants.
    /// Structural AST pattern match per plan §Dimension-Unit Consistency Validation.
    /// </summary>
    private static void ValidateUnitSlotDimensionConsistency(
        TypedExpression holeExpr,
        ImmutableArray<DeclaredQualifierMeta>? targetQualifiers,
        SourceSpan span,
        CheckContext ctx)
    {
        var sourceDimension = ResolveSlotSourceQualifierAxis(holeExpr, QualifierAxis.Dimension, out var sourceName);
        if (sourceDimension.Kind != QualifierResolutionKind.Resolved
            || sourceDimension.Qualifier is null
            || !TryGetQualifierText(sourceDimension.Qualifier, QualifierAxis.Dimension, out var sourceDimensionName)
            || targetQualifiers is not { } tq
            || tq.IsDefaultOrEmpty)
        {
            return;
        }

        var expandedTargetQualifiers = ExpandAssignmentTargetQualifiers(tq);
        var targetDimension = expandedTargetQualifiers
            .Select(q => ProjectQualifierForAxis(q, QualifierAxis.Dimension))
            .OfType<DeclaredQualifierMeta.Dimension>()
            .Select(q => q.DimensionName)
            .FirstOrDefault();

        if (targetDimension is null)
            return;

        if (!string.Equals(sourceDimensionName, targetDimension, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.DimensionMismatchInUnitSlot, span,
                    sourceName, sourceDimensionName, targetDimension));
        }
    }

    /// <summary>
    /// Resolve an interpolated string expression. Resolves each hole segment's expression.
    /// ErrorType propagation: if any hole is error, the entire string is <see cref="TypedErrorExpression"/>.
    /// Result type is always <see cref="TypeKind.String"/>.
    /// </summary>
    private static TypedExpression ResolveInterpolatedString(InterpolatedStringExpression expr, CheckContext ctx)
    {
        var segments = ImmutableArray.CreateBuilder<TypedInterpolationSegment>(expr.Segments.Length);
        bool hasError = false;

        foreach (var segment in expr.Segments)
        {
            switch (segment)
            {
                case TextSegment text:
                    segments.Add(new TypedTextSegment(text.Text, text.Span));
                    break;
                case HoleSegment hole:
                    var resolved = Resolve(hole.Expression, ctx);
                    if (resolved is TypedErrorExpression)
                        hasError = true;
                    segments.Add(new TypedHoleSegment(resolved, hole.Span));
                    break;
            }
        }

        if (hasError)
            return new TypedErrorExpression(expr.Span);

        return new TypedInterpolatedString(segments.ToImmutable(), expr.Span);
    }
}

