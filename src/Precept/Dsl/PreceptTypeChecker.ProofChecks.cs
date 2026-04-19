using System;
using System.Collections.Generic;

namespace Precept;

// Partial-class file for PreceptTypeChecker — proof-check logic.
// Contains interval inference, proof-backed assessments, and proof-analysis helpers
// that back the divisor-safety (C92/C93), guard-tautology (C97/C98), and
// sqrt-argument (C94/C95) diagnostic checks.
internal static partial class PreceptTypeChecker
{

    /// <summary>
    /// Extracts the tightest <see cref="NumericInterval"/> for <paramref name="key"/> from the
    /// typed proof stores. Used by <c>TryInferInterval</c> (Layer 3) to initialize leaf intervals.
    /// Priority: typed interval store → flag-based interval (Positive → Nonneg+Nonzero → Nonneg) → Unknown.
    /// </summary>
    private static NumericInterval ExtractFieldInterval(
        string key,
        GlobalProofContext context)
    {
        // 1. Typed interval store takes priority (most specific).
        if (context.FieldIntervals.TryGetValue(key, out var ival))
            return ival;

        // 2. Flag-based interval.
        if (context.Flags.TryGetValue(key, out var flags))
        {
            if ((flags & NumericFlags.Positive) != 0)
                return NumericInterval.Positive;
            if ((flags & NumericFlags.Nonnegative) != 0 && (flags & NumericFlags.Nonzero) != 0)
                return NumericInterval.Positive; // nonneg + nonzero = strictly positive
            if ((flags & NumericFlags.Nonnegative) != 0)
                return NumericInterval.Nonneg;
        }

        return NumericInterval.Unknown;
    }

    /// <summary>
    /// Recursively infers the <see cref="NumericInterval"/> for <paramref name="expression"/>
    /// using Layer 2 transfer rules (interval arithmetic) and Layer 5 conditional hull synthesis.
    /// Each identifier leaf is initialized via <see cref="ExtractFieldInterval"/>.
    /// Returns <see cref="NumericInterval.Unknown"/> for any expression whose bounds cannot be determined.
    /// </summary>
    internal static NumericInterval TryInferInterval(
        PreceptExpression expression,
        GlobalProofContext context)
    {
        switch (expression)
        {
            case PreceptLiteralExpression { Value: long l }:
                return new NumericInterval(l, true, l, true);
            case PreceptLiteralExpression { Value: double d }:
                return new NumericInterval(d, true, d, true);
            case PreceptLiteralExpression { Value: decimal m }:
                return new NumericInterval((double)m, true, (double)m, true);

            case PreceptIdentifierExpression idExpr:
                if (TryGetIdentifierKey(idExpr, out var idKey))
                    return ExtractFieldInterval(idKey, context);
                return NumericInterval.Unknown;

            case PreceptParenthesizedExpression paren:
                return TryInferInterval(paren.Inner, context);

            case PreceptUnaryExpression { Operator: "-" } unary:
                return NumericInterval.Negate(TryInferInterval(unary.Operand, context));

            case PreceptBinaryExpression binary:
            {
                // Use IntervalOf (not TryInferInterval) on sub-expressions so relational facts
                // are consulted for each leaf before combining (e.g. (A-B)*C with rule A>B, C positive).
                var left  = context.IntervalOf(binary.Left).Interval;
                var right = context.IntervalOf(binary.Right).Interval;
                return binary.Operator switch
                {
                    "+" => NumericInterval.Add(left, right),
                    "-" => NumericInterval.Subtract(left, right),
                    "*" => NumericInterval.Multiply(left, right),
                    "/" => NumericInterval.Divide(left, right),
                    "%" => right.ExcludesZero
                             ? (left.IsNonnegative && right.IsPositive
                                 ? new NumericInterval(0, true, Math.Abs(right.Upper), false) // [0, |B|) — tighter for non-negative dividend
                                 : new NumericInterval(-Math.Abs(right.Upper), false, Math.Abs(right.Upper), false))
                             : NumericInterval.Unknown,
                    _   => NumericInterval.Unknown,
                };
            }

            case PreceptFunctionCallExpression fn when fn.Arguments.Length >= 1:
            {
                var a0 = TryInferInterval(fn.Arguments[0], context);
                switch (fn.Name)
                {
                    case "abs":
                        return NumericInterval.Abs(a0);
                    case "min" when fn.Arguments.Length == 2:
                        return NumericInterval.Min(a0, TryInferInterval(fn.Arguments[1], context));
                    case "max" when fn.Arguments.Length == 2:
                        return NumericInterval.Max(a0, TryInferInterval(fn.Arguments[1], context));
                    case "clamp" when fn.Arguments.Length == 3:
                        return NumericInterval.Clamp(a0,
                            TryInferInterval(fn.Arguments[1], context),
                            TryInferInterval(fn.Arguments[2], context));
                    case "sqrt":
                        if (a0.IsNonnegative)
                        {
                            var sqrtLo = Math.Sqrt(Math.Max(0, a0.Lower));
                            var sqrtHi = double.IsPositiveInfinity(a0.Upper)
                                ? double.PositiveInfinity
                                : Math.Sqrt(a0.Upper);
                            return new NumericInterval(sqrtLo, a0.LowerInclusive, sqrtHi, a0.UpperInclusive);
                        }
                        return NumericInterval.Unknown;
                    case "floor":
                        return new NumericInterval(Math.Floor(a0.Lower), true, Math.Floor(a0.Upper), true);
                    case "ceil":
                        return new NumericInterval(Math.Ceiling(a0.Lower), true, Math.Ceiling(a0.Upper), true);
                    case "round":
                        return new NumericInterval(Math.Floor(a0.Lower), true, Math.Ceiling(a0.Upper), true);
                    case "truncate":
                        if (a0.IsUnknown) return NumericInterval.Unknown;
                        var tLo = a0.Lower >= 0 ? Math.Floor(a0.Lower) : Math.Ceiling(a0.Lower);
                        var tHi = a0.Upper >= 0 ? Math.Floor(a0.Upper) : Math.Ceiling(a0.Upper);
                        return new NumericInterval(Math.Min(tLo, tHi), true, Math.Max(tLo, tHi), true);
                    case "pow" when fn.Arguments.Length == 2:
                        var exp = TryInferInterval(fn.Arguments[1], context);
                        // Only handle constant integer exponents.
                        if (exp.Lower != exp.Upper || exp.Lower != Math.Floor(exp.Lower))
                            return NumericInterval.Unknown;
                        int n = (int)exp.Lower;
                        if (n % 2 == 0)
                        {
                            // Even exponent: result is nonneg.
                            if (a0.IsNonnegative)
                                return new NumericInterval(Math.Pow(a0.Lower, n), true, Math.Pow(a0.Upper, n), true);
                            if (a0.Upper <= 0)
                                return new NumericInterval(Math.Pow(a0.Upper, n), true, Math.Pow(a0.Lower, n), true);
                            // Mixed sign: [0, max(lo^n, hi^n)]
                            var maxPow = Math.Max(Math.Pow(a0.Lower, n), Math.Pow(a0.Upper, n));
                            bool baseNonzero = fn.Arguments[0] is PreceptIdentifierExpression baseId
                                && context.Flags.TryGetValue(
                                    baseId.Member is not null ? $"{baseId.Name}.{baseId.Member}" : baseId.Name,
                                    out var baseFlags)
                                && (baseFlags & (NumericFlags.Nonzero | NumericFlags.Positive)) != 0;
                            return new NumericInterval(0, !baseNonzero, maxPow, true);
                        }
                        else
                        {
                            // Odd exponent: preserves monotonicity.
                            return new NumericInterval(Math.Pow(a0.Lower, n), true, Math.Pow(a0.Upper, n), true);
                        }
                    default:
                        return NumericInterval.Unknown;
                }
            }

            case PreceptConditionalExpression cond:
            {
                var thenContext = ApplyNarrowing(cond.Condition, context, assumeTrue: true);
                var elseContext = ApplyNarrowing(cond.Condition, context, assumeTrue: false);
                // Use IntervalOf (not TryInferInterval) so relational facts stored by ApplyNarrowing
                // are consulted when tightening branch sub-expressions (e.g. A-B given A>B).
                var thenInterval = thenContext.IntervalOf(cond.ThenBranch).Interval;
                var elseInterval = elseContext.IntervalOf(cond.ElseBranch).Interval;
                return NumericInterval.Hull(thenInterval, elseInterval);
            }

            default:
                return NumericInterval.Unknown;
        }
    }

    private static string FlipComparisonOperator(string op) => op switch
    {
        "<" => ">",
        "<=" => ">=",
        ">" => "<",
        ">=" => "<=",
        _ => op // == and != are symmetric
    };

    /// <summary>
    /// Assesses divisor safety using the unified proof model.
    /// Returns <c>null</c> when no diagnostic is needed (non-numeric or non-division context).
    /// </summary>
    private static ProofAssessment? AssessDivisorSafety(
        PreceptExpression divisor, GlobalProofContext context)
    {
        var proofResult = context.IntervalOf(divisor);
        var interval = proofResult.Interval;
        var attribution = proofResult.Attribution;
        var subject = DescribeExpression(divisor);

        // Provably zero: contradiction → C92
        if (interval is { Lower: 0, Upper: 0, LowerInclusive: true, UpperInclusive: true })
            return new ProofAssessment(
                ProofRequirement.NonzeroDivisor, ProofOutcome.Contradiction,
                subject, interval, attribution);

        // Provably nonzero via interval: satisfied → no diagnostic
        if (interval.ExcludesZero)
            return new ProofAssessment(
                ProofRequirement.NonzeroDivisor, ProofOutcome.Satisfied,
                subject, interval, attribution);

        // Flag-based nonzero proof for identifiers: the Nonzero flag (from rule != 0,
        // when != 0, etc.) cannot be expressed as a single contiguous interval, so
        // check the flags dictionary directly for identifier expressions.
        if (TryGetIdentifierKey(divisor, out var key) &&
            context.Flags.TryGetValue(key, out var flags) &&
            (flags & (NumericFlags.Nonzero | NumericFlags.Positive)) != 0)
            return new ProofAssessment(
                ProofRequirement.NonzeroDivisor, ProofOutcome.Satisfied,
                subject, interval, attribution);

        // Obligation: zero still possible or unknown → C93
        return new ProofAssessment(
            ProofRequirement.NonzeroDivisor, ProofOutcome.Obligation,
            subject, interval, attribution);
    }

    /// <summary>
    /// Assesses a guard expression for C97 (dead guard — always false) and
    /// C98 (tautological guard — always true) using field constraint intervals.
    /// Only handles simple single-field comparisons (e.g. <c>when X &lt; 0</c>).
    /// </summary>
    private static void AssessGuard(
        PreceptExpression guard,
        string guardText,
        int sourceLine,
        IReadOnlyDictionary<string, NumericInterval> fieldIntervals,
        List<PreceptValidationDiagnostic> diagnostics,
        string? stateContext)
    {
        if (!TryExtractSingleFieldComparison(guard, out var fieldName, out var guardTrueInterval))
            return;

        if (!fieldIntervals.TryGetValue(fieldName, out var constraintIval) || constraintIval.IsUnknown)
            return;

        // C97: guard's "true" interval is disjoint from field constraints → always false
        if (NumericInterval.AreDisjoint(guardTrueInterval, constraintIval))
        {
            var assessment = new ProofAssessment(
                ProofRequirement.GuardSatisfiability, ProofOutcome.Contradiction,
                fieldName, guardTrueInterval, ProofAttribution.None,
                ConstraintInterval: constraintIval,
                ConstraintDescription: guardText.Trim());
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C97,
                ProofDiagnosticRenderer.Render(assessment),
                sourceLine,
                Column: guard.Position?.StartColumn ?? 0,
                EndColumn: guard.Position?.EndColumn ?? 0,
                StateContext: stateContext,
                Assessment: assessment));
            return;
        }

        // C98: field constraints are entirely within the guard's "true" interval → always true
        if (NumericInterval.Contains(guardTrueInterval, constraintIval))
        {
            var assessment = new ProofAssessment(
                ProofRequirement.GuardTautology, ProofOutcome.Satisfied,
                fieldName, guardTrueInterval, ProofAttribution.None,
                ConstraintInterval: constraintIval,
                ConstraintDescription: guardText.Trim());
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C98,
                ProofDiagnosticRenderer.Render(assessment),
                sourceLine,
                Column: guard.Position?.StartColumn ?? 0,
                EndColumn: guard.Position?.EndColumn ?? 0,
                StateContext: stateContext,
                Assessment: assessment));
        }
    }

    /// <summary>
    /// Extracts a single field-vs-literal comparison from a rule expression (e.g. <c>X &lt; 5</c>)
    /// and returns the field name plus the numeric interval the comparison implies for that field.
    /// Returns <c>false</c> when the expression is not a simple <c>identifier op literal</c> form.
    /// </summary>
    private static bool TryExtractSingleFieldComparison(
        PreceptExpression expr,
        out string fieldName,
        out NumericInterval ruleInterval)
    {
        fieldName = default!;
        ruleInterval = default;

        if (expr is not PreceptBinaryExpression bin) return false;

        // One side must be identifier, other must be numeric literal
        string? name = null;
        double? value = null;
        bool identifierOnLeft = false;

        if (bin.Left is PreceptIdentifierExpression id && id.Member is null
            && bin.Right is PreceptLiteralExpression lit && TryGetNumericValue(lit.Value, out var rv))
        {
            name = id.Name;
            value = rv;
            identifierOnLeft = true;
        }
        else if (bin.Right is PreceptIdentifierExpression id2 && id2.Member is null
                 && bin.Left is PreceptLiteralExpression lit2 && TryGetNumericValue(lit2.Value, out var lv))
        {
            name = id2.Name;
            value = lv;
            identifierOnLeft = false;
        }

        static bool TryGetNumericValue(object? val, out double result)
        {
            switch (val)
            {
                case double d: result = d; return true;
                case long l: result = l; return true;
                case int i: result = i; return true;
                default: result = 0; return false;
            }
        }

        if (name is null || value is null) return false;

        fieldName = name;

        // Convert comparison to interval the field must satisfy.
        // identifierOnLeft: "X op value" → interval for X
        // !identifierOnLeft: "value op X" → flip the operator for X
        var op = bin.Operator;
        if (!identifierOnLeft)
        {
            op = FlipComparisonOperator(op);
        }

        ruleInterval = op switch
        {
            "<" => new NumericInterval(double.NegativeInfinity, false, value.Value, false),
            "<=" => new NumericInterval(double.NegativeInfinity, false, value.Value, true),
            ">" => new NumericInterval(value.Value, false, double.PositiveInfinity, false),
            ">=" => new NumericInterval(value.Value, true, double.PositiveInfinity, false),
            "==" => new NumericInterval(value.Value, true, value.Value, true),
            "!=" => NumericInterval.Unknown, // Can't express "not equal" as single interval
            _ => NumericInterval.Unknown,
        };

        return !ruleInterval.IsUnknown;
    }

    /// <summary>
    /// Returns a human-readable description of an expression for use in diagnostic messages.
    /// </summary>
    private static string DescribeExpression(PreceptExpression expr) => expr switch
    {
        PreceptLiteralExpression lit => lit.Value?.ToString() ?? "null",
        PreceptIdentifierExpression id when id.Member is null => id.Name,
        PreceptIdentifierExpression id => $"{id.Name}.{id.Member}",
        _ => "expression",
    };

    /// <summary>
    /// Assesses whether a sqrt() argument is provably non-negative using the unified proof model.
    /// Returns <c>null</c> when no diagnostic is needed (argument is provably non-negative).
    /// </summary>
    private static ProofAssessment? AssessNonnegativeArgument(
        PreceptExpression arg, GlobalProofContext context)
    {
        var subject = DescribeExpression(arg);

        // Check literals directly.
        if (arg is PreceptLiteralExpression { Value: long lval } && lval >= 0)
            return null;
        if (arg is PreceptLiteralExpression { Value: double dval } && dval >= 0)
            return null;
        if (arg is PreceptLiteralExpression { Value: decimal mval } && mval >= 0)
            return null;

        if (context.KnowsNonnegative(arg))
            return null;

        var proofResult = context.IntervalOf(arg);
        var interval = proofResult.Interval;

        // Provably negative: contradiction
        if (interval is { Upper: < 0 })
            return new ProofAssessment(
                ProofRequirement.NonnegativeArgument, ProofOutcome.Contradiction,
                subject, interval, proofResult.Attribution);

        // Obligation: may be negative
        return new ProofAssessment(
            ProofRequirement.NonnegativeArgument, ProofOutcome.Obligation,
            subject, interval, proofResult.Attribution);
    }

    private static bool TryGetNumericLiteral(PreceptExpression expr, out double value)
    {
        var stripped = StripParentheses(expr);
        if (stripped is PreceptLiteralExpression { Value: long l })
        {
            value = (double)l;
            return true;
        }
        if (stripped is PreceptLiteralExpression { Value: double d })
        {
            value = d;
            return true;
        }
        value = 0;
        return false;
    }
}
