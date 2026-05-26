using System.Linq;
using System.Runtime.CompilerServices;
using NodaTime;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Canonical static-magnitude projection for typed expressions. Extracts the
/// declared decimal magnitude from a typed literal, typed constant, or
/// interpolated typed constant — covering bare numerics, business-magnitude
/// tuples (money, quantity, price, exchangerate), and temporal types
/// (duration, period). Returns false when the expression has no compile-time
/// magnitude.
///
/// For <see cref="TypedTypedConstant"/>, the returned magnitude is the raw
/// declared value (tuple[0]) — callers that need unit-normalized comparison
/// apply <see cref="TypedConstantNormalizer"/> on top. For
/// <see cref="InterpolatedTypedConstant"/>, the static-unit factor is already
/// folded in (no raw form exists without the unit context).
///
/// Period projection returns the sign of the FIRST non-zero component
/// (years dominate months dominate weeks ...). Sufficient for
/// nonnegative/positive/nonzero on single-unit and same-sign mixed-unit
/// periods. Mixed-SIGN periods (e.g., '1 year + -13 months') misclassify —
/// the leading component's sign wins even when the calendar-effective
/// magnitude has the opposite sign. NOT suitable for min/max bound
/// comparison. Period bounds need a separate design.
/// </summary>
internal static class TypedExpressionMagnitude
{
    public static bool TryGetStaticMagnitude(TypedExpression expression, out decimal magnitude)
    {
        switch (expression)
        {
            case TypedLiteral { Value: var value }:
                return TryFromBoxedNumeric(value, out magnitude);

            case TypedTypedConstant { ParsedValue: var parsed }:
                return TryGetMagnitudeFromParsedValue(parsed, out magnitude);

            case InterpolatedTypedConstant itc:
                return TryGetInterpolatedMagnitude(itc, out magnitude);

            case TypedUnaryOp { ResolvedOp: var op, Operand: var operand }
                when IsNegateOp(op) && TryGetStaticMagnitude(operand, out var inner):
                magnitude = -inner;
                return true;
        }

        magnitude = default;
        return false;
    }

    public static bool TryGetMagnitudeFromParsedValue(object? parsedValue, out decimal magnitude)
    {
        if (TryFromBoxedNumeric(parsedValue, out magnitude))
            return true;

        if (parsedValue is ITuple tuple && tuple.Length > 0 && tuple[0] is decimal tupleMagnitude)
        {
            magnitude = tupleMagnitude;
            return true;
        }

        if (parsedValue is Duration duration)
        {
            magnitude = duration.BclCompatibleTicks;
            return true;
        }

        if (parsedValue is Period period)
        {
            magnitude = ProjectPeriodSign(period);
            return true;
        }

        magnitude = default;
        return false;
    }

    private static bool TryFromBoxedNumeric(object? value, out decimal magnitude)
    {
        switch (value)
        {
            case decimal d: magnitude = d; return true;
            case int i:     magnitude = i; return true;
            case long l:    magnitude = l; return true;
        }
        magnitude = default;
        return false;
    }

    private static bool TryGetInterpolatedMagnitude(InterpolatedTypedConstant itc, out decimal magnitude)
    {
        if (itc.StaticMagnitude is not { } staticMagnitude)
        {
            magnitude = default;
            return false;
        }

        var staticUnit = itc.StaticQualifier switch
        {
            StaticUnitQualifier { Unit: var u } => u,
            StaticCurrencyAndUnitQualifier { Unit: var u } => u,
            _ => null,
        };

        if (staticUnit is null)
        {
            if (itc.Slots.Any(slot => slot.SlotKind is InterpolationSlotKind.Unit
                                                  or InterpolationSlotKind.NumeratorUnit
                                                  or InterpolationSlotKind.DenominatorUnit))
            {
                if (staticMagnitude == 0m)
                {
                    magnitude = 0m;
                    return true;
                }
                magnitude = default;
                return false;
            }

            magnitude = staticMagnitude;
            return true;
        }

        var factor = TypedConstantNormalizer.TryGetStaticScalingFactor(staticUnit);
        if (!factor.HasValue)
        {
            magnitude = default;
            return false;
        }

        magnitude = factor.Value * staticMagnitude;
        return true;
    }

    private static bool IsNegateOp(OperationKind op) =>
        Operations.GetMeta(op) is UnaryOperationMeta { Op: OperatorKind.Negate };

    private static decimal ProjectPeriodSign(Period p)
    {
        if (p.Years       != 0) return p.Years       * 1_000_000_000_000m;
        if (p.Months      != 0) return p.Months      * 1_000_000_000m;
        if (p.Weeks       != 0) return p.Weeks       * 100_000_000m;
        if (p.Days        != 0) return p.Days        * 10_000_000m;
        if (p.Hours       != 0) return p.Hours       * 1_000_000m;
        if (p.Minutes     != 0) return p.Minutes     * 10_000m;
        if (p.Seconds     != 0) return p.Seconds     * 100m;
        if (p.Milliseconds != 0) return p.Milliseconds * 1m;
        if (p.Ticks       != 0) return p.Ticks       * 0.0001m;
        if (p.Nanoseconds != 0) return p.Nanoseconds * 0.0000001m;
        return 0m;
    }
}
