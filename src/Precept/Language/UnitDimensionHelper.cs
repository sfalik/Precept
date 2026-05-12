using System.Collections.Frozen;

namespace Precept.Language;

internal static class UnitDimensionHelper
{
    internal static readonly FrozenSet<string> CountQualifierUnitCodes = new[]
    {
        "1",
        "%",
        "[ppm]",
        "[ppb]",
        "[ppth]",
        "[pptr]",
        "each",
        "[iU]",
        "[arb'U]",
        "[USP'U]",
        "[CFU]",
        "[pH]",
        "dB",
        // Precept-defined count/logistics atoms
        "piece",
        "unit",
        "pkg",
        "box",
        "case",
        "carton",
        "pallet",
        "dozen",
        "gross",
        "bag",
        "bundle",
        "roll",
        "sheet",
        "drum",
        "container",
        "tote",
        "bin",
        "tablet",
        "capsule",
        "dose",
        "item",
        "pair",
        "set",
    }.ToFrozenSet(StringComparer.Ordinal);

    internal static readonly FrozenSet<string> NonCountDimensionlessUnitCodes = new[]
    {
        "rad",
        "deg",
        "'",
        "''",
        "gon",
        "sr",
    }.ToFrozenSet(StringComparer.Ordinal);

    internal static string DeriveUnitDimensionName(UcumParsedUnit unit)
    {
        if (!unit.Vector.IsDimensionless)
            return unit.PreferredDimensionAlias ?? "";

        if (CountQualifierUnitCodes.Contains(unit.CanonicalCode))
            return "count";

        if (IsNonCountDimensionlessUnit(unit))
            return "";

        return DimensionCatalog.TryGetAlias(unit.Vector, out var alias) && alias is not null
            ? alias.Name
            : unit.PreferredDimensionAlias ?? "";
    }

    internal static bool TryGetCanonicalCompoundUnitCode(string value, out string canonicalCode)
    {
        canonicalCode = string.Empty;

        var slashIndex = value.IndexOf('/');
        if (slashIndex <= 0 || slashIndex != value.LastIndexOf('/'))
            return false;

        var numerator = value[..slashIndex];
        var denominator = value[(slashIndex + 1)..];
        if (!TryGetCanonicalUnitCodePreservingCountAtoms(numerator, out var canonicalNumerator)
            || !TryGetCanonicalUnitCodePreservingCountAtoms(denominator, out var canonicalDenominator))
        {
            return false;
        }

        canonicalCode = $"{canonicalNumerator}/{canonicalDenominator}";
        return true;
    }

    private static bool TryGetCanonicalUnitCodePreservingCountAtoms(string value, out string canonicalCode)
    {
        if (CountQualifierUnitCodes.Contains(value) || NonCountDimensionlessUnitCodes.Contains(value))
        {
            canonicalCode = value;
            return true;
        }

        var result = UcumParser.Parse(value);
        if (!result.IsValid || result.Unit is null)
        {
            canonicalCode = string.Empty;
            return false;
        }

        canonicalCode = result.Unit.CanonicalCode;
        return canonicalCode.IndexOf('/') < 0;
    }

    private static bool IsNonCountDimensionlessUnit(UcumParsedUnit unit) =>
        NonCountDimensionlessUnitCodes.Contains(unit.CanonicalCode)
        || unit.UsedAtoms.Any(atom => NonCountDimensionlessUnitCodes.Contains(atom.Code));
}
