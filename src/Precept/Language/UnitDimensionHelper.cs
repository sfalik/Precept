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

    private static bool IsNonCountDimensionlessUnit(UcumParsedUnit unit) =>
        NonCountDimensionlessUnitCodes.Contains(unit.CanonicalCode)
        || unit.UsedAtoms.Any(atom => NonCountDimensionlessUnitCodes.Contains(atom.Code));
}
