using System;
using Precept.Language;

namespace Precept.Pipeline;

internal static class QualifierUnitHelpers
{
    internal static bool TrySplitCompoundUnit(string unitCode, out string numeratorUnit, out string denominatorUnit)
    {
        var slashIndex = unitCode.IndexOf('/');
        if (slashIndex <= 0 || slashIndex != unitCode.LastIndexOf('/'))
        {
            numeratorUnit = string.Empty;
            denominatorUnit = string.Empty;
            return false;
        }

        numeratorUnit = unitCode[..slashIndex];
        denominatorUnit = unitCode[(slashIndex + 1)..];
        return numeratorUnit.Length > 0 && denominatorUnit.Length > 0;
    }

    internal static bool TryDeriveUnitDimensionName(string unitCode, out string dimensionName)
    {
        if (UnitDimensionHelper.CountQualifierUnitCodes.Contains(unitCode))
        {
            dimensionName = "count";
            return true;
        }

        if (unitCode.StartsWith("{", StringComparison.Ordinal)
            && unitCode.EndsWith("}", StringComparison.Ordinal)
            && unitCode.IndexOf('/') < 0)
        {
            dimensionName = $"{unitCode[..^1]}.dimension}}";
            return true;
        }

        var result = UcumParser.Parse(unitCode);
        if (result.IsValid)
        {
            dimensionName = UnitDimensionHelper.DeriveUnitDimensionName(result.Unit!);
            return !string.IsNullOrWhiteSpace(dimensionName);
        }

        dimensionName = string.Empty;
        return false;
    }
}
