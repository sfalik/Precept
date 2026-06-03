using System;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // ════════════════════════════════════════════════════════════════════════
    //  String Length Containment and Collection Count Containment
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to prove a <see cref="LengthContainmentProofRequirement"/> obligation by computing
    /// a static string-length interval for the assigned RHS and checking it against the declared
    /// minlength/maxlength. Covers literals, field/arg references, concatenation, conditionals, and
    /// the length-stable string functions (see <see cref="StringLengthIntervalOf"/>).
    /// </summary>
    /// <returns>
    /// <c>true</c> when the RHS length interval is provably within <c>[declaredMin .. declaredMax]</c>;
    /// <c>false</c> when it provably violates (interval below declaredMin, or above declaredMax);
    /// <c>null</c> (unresolved) when the interval cannot establish containment — e.g. an unbounded
    /// upper bound against a declared maxlength. Per §0.7 prove-or-reject, both <c>false</c> and
    /// <c>null</c> leave the obligation unresolved so the diagnostic is emitted.
    /// </returns>
    internal static bool? TryLengthContainmentProof(
        LengthContainmentProofRequirement req,
        TypedExpression site,
        SemanticIndex semantics)
    {
        var (min, max) = StringLengthIntervalOf(site, semantics);

        // Provably violating: the whole interval lies outside the declared band.
        if (req.DeclaredMaxLength.HasValue && min > req.DeclaredMaxLength.Value)
            return false; // shortest possible result already exceeds maxlength
        if (req.DeclaredMinLength.HasValue && max.HasValue && max.Value < req.DeclaredMinLength.Value)
            return false; // longest possible result is still below minlength

        // Provably contained: interval fully inside the declared band.
        bool minOk = !req.DeclaredMinLength.HasValue || min >= req.DeclaredMinLength.Value;
        bool maxOk = !req.DeclaredMaxLength.HasValue || (max.HasValue && max.Value <= req.DeclaredMaxLength.Value);
        if (minOk && maxOk)
            return true;

        // Not provable (e.g. unbounded max vs a declared maxlength): unresolved ⇒ emit.
        return null;
    }

    /// <summary>
    /// Computes the static character-length interval <c>(min, max)</c> of a string-typed expression.
    /// <c>max</c> is <c>null</c> when the upper bound is not statically knowable (unbounded). Mirrors
    /// the numeric <see cref="IntervalOfNarrowed"/> shape: literals are points, references read their
    /// declared length modifiers, concatenation sums operand intervals, conditionals union branches,
    /// and length-stable string functions transfer their argument's interval.
    /// </summary>
    internal static (int min, int? max) StringLengthIntervalOf(TypedExpression expr, SemanticIndex semantics)
    {
        switch (expr)
        {
            // String literal → exact length.
            case TypedLiteral { ResultType: TypeKind.String, Value: string s }:
                return (s.Length, s.Length);

            // Field reference → its declared length modifiers. notempty ⇒ min ≥ 1.
            case TypedFieldRef fieldRef when fieldRef.ResultType == TypeKind.String:
            {
                if (!semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out var field))
                    return (0, null);
                bool notEmpty = !field.Modifiers.IsDefaultOrEmpty && field.Modifiers.Contains(ModifierKind.Notempty);
                return LengthIntervalFromModifiers(field.DeclaredMinLength, field.DeclaredMaxLength, notEmpty);
            }

            // Dotted arg reference (E.Arg) → the arg's declared length modifiers. notempty ⇒ min ≥ 1.
            case TypedArgRef argRef when argRef.ResultType == TypeKind.String:
            {
                if (!semantics.EventsByName.TryGetValue(argRef.EventName, out var evt))
                    return (0, null);
                foreach (var arg in evt.Args)
                {
                    if (!string.Equals(arg.Name, argRef.ArgName, StringComparison.Ordinal))
                        continue;
                    bool notEmpty = !arg.Modifiers.IsDefaultOrEmpty && arg.Modifiers.Contains(ModifierKind.Notempty);
                    return LengthIntervalFromModifiers(arg.DeclaredMinLength, arg.DeclaredMaxLength, notEmpty);
                }
                return (0, null);
            }

            // Concatenation a + b → sum of operand intervals; unbounded if either operand is unbounded.
            case TypedBinaryOp { ResolvedOp: OperationKind.StringPlusString } bin:
            {
                var (lMin, lMax) = StringLengthIntervalOf(bin.Left, semantics);
                var (rMin, rMax) = StringLengthIntervalOf(bin.Right, semantics);
                int min = lMin + rMin;
                int? max = (lMax.HasValue && rMax.HasValue) ? lMax.Value + rMax.Value : (int?)null;
                return (min, max);
            }

            // if c then a else b → (min(minA,minB), unbounded unless both branches are bounded).
            case TypedConditional { ResultType: TypeKind.String } cond:
            {
                var (tMin, tMax) = StringLengthIntervalOf(cond.ThenBranch, semantics);
                var (eMin, eMax) = StringLengthIntervalOf(cond.ElseBranch, semantics);
                int min = Math.Min(tMin, eMin);
                int? max = (tMax.HasValue && eMax.HasValue) ? Math.Max(tMax.Value, eMax.Value) : (int?)null;
                return (min, max);
            }

            // Length-stable string functions.
            case TypedFunctionCall { ResultType: TypeKind.String } call:
                return StringFunctionLengthInterval(call, semantics);

            // Interpolated string 'lit {hole} lit' → sum of literal segment char counts and
            // each hole's length interval. A hole with an unbounded interval makes the whole
            // result unbounded.
            case TypedInterpolatedString interp:
                return InterpolatedStringLengthInterval(interp, semantics);

            // Collection member access reading an element (e.g. queue .peek, list .first/.last):
            // the result is one element, whose length interval is the element type's declared
            // length bounds when known. Today element-type length modifiers are not expressible
            // (PRE0033), so this is unbounded — kept as the sound place to read them once they are.
            case TypedMemberAccess access when access.ResultType == TypeKind.String:
                return MemberAccessStringLengthInterval(access, semantics);

            // Anything else (unknown): unbounded.
            default:
                return (0, null);
        }
    }

    /// <summary>
    /// Length interval of an interpolated string: the sum of the literal text-segment character
    /// counts plus each hole's length interval. A string-typed hole contributes its
    /// <see cref="StringLengthIntervalOf"/>; a numeric hole with a statically-known integer range
    /// contributes its maximum decimal digit count (including a sign character when the range can
    /// be negative). Any hole with no static bound makes the whole result unbounded — never
    /// under-estimate a length.
    /// </summary>
    private static (int min, int? max) InterpolatedStringLengthInterval(TypedInterpolatedString interp, SemanticIndex semantics)
    {
        int min = 0;
        int? max = 0;
        foreach (var segment in interp.Segments)
        {
            switch (segment)
            {
                case TypedTextSegment text:
                    min += text.Text.Length;
                    if (max.HasValue) max = max.Value + text.Text.Length;
                    break;

                case TypedHoleSegment hole:
                {
                    var (hMin, hMax) = HoleLengthInterval(hole.Expression, semantics);
                    min += hMin;
                    max = (max.HasValue && hMax.HasValue) ? max.Value + hMax.Value : (int?)null;
                    break;
                }
            }
        }
        return (min, max);
    }

    /// <summary>
    /// Length interval of an interpolation hole. String-typed holes defer to
    /// <see cref="StringLengthIntervalOf"/>. Numeric/temporal/other holes are rendered as text:
    /// when the hole's static numeric interval is bounded, the upper bound is the maximum decimal
    /// digit count of the range (plus one for a sign when the range reaches negative); otherwise
    /// unbounded. The lower bound is 0 (a hole could render empty for an optional/edge value),
    /// which is sound for a maxlength-direction proof.
    /// </summary>
    private static (int min, int? max) HoleLengthInterval(TypedExpression hole, SemanticIndex semantics)
    {
        if (hole.ResultType == TypeKind.String)
            return StringLengthIntervalOf(hole, semantics);

        var interval = IntervalOf(hole, semantics);
        if (interval.IsUnbounded || interval.IsEmpty)
            return (0, null);

        int? digits = DecimalDigitWidth(interval.Min, interval.Max);
        return (0, digits);
    }

    /// <summary>
    /// Maximum number of characters needed to render any value in the closed interval
    /// <c>[min, max]</c> as a decimal — the larger of the two endpoints' decimal widths, plus one
    /// for a leading sign when the interval can be negative. Conservative for fractional magnitudes:
    /// a non-integer interval renders unbounded (returns <c>null</c>) because the decimal expansion
    /// length is not statically knowable.
    /// </summary>
    private static int? DecimalDigitWidth(decimal min, decimal max)
    {
        if (decimal.Truncate(min) != min || decimal.Truncate(max) != max)
            return null; // fractional values: rendered length not statically bounded

        int widthOf(decimal v)
        {
            // Number of decimal digits of |v| (at least 1 for zero).
            decimal abs = Math.Abs(v);
            int d = 1;
            while (abs >= 10m) { abs /= 10m; d++; }
            return d;
        }

        int width = Math.Max(widthOf(min), widthOf(max));
        if (min < 0m) width += 1; // sign character
        return width;
    }

    /// <summary>
    /// Length interval for a collection element read via member access (e.g. <c>.peek</c>,
    /// <c>.first</c>, <c>.last</c>). The element's length interval is the receiver collection
    /// field's element-type declared length bounds when present; otherwise unbounded. This makes
    /// a <c>maxlength</c>-bounded string element read provable into a same-or-wider-capped field
    /// (the read-site reach that closes the element-bound length gap). A collection with no
    /// element bound stays unbounded — the prior behavior is preserved exactly.
    /// </summary>
    private static (int min, int? max) MemberAccessStringLengthInterval(TypedMemberAccess access, SemanticIndex semantics)
    {
        if (access.Object is TypedFieldRef fieldRef
            && semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out var field)
            && field.ElementType?.ValueBounds is { IsEmpty: false } bounds)
        {
            return LengthIntervalFromModifiers(bounds.DeclaredMinLength, bounds.DeclaredMaxLength, bounds.NotEmpty);
        }

        return (0, null);
    }

    /// <summary>
    /// Builds a length interval from a field/arg's declared length modifiers. <c>notempty</c> folds
    /// to a <c>minlength 1</c> lower bound even when no explicit minlength is declared (it sets no
    /// DeclaredMinLength; see the default-value length stamp in <c>ProofEngine.Analysis.cs</c>).
    /// </summary>
    private static (int min, int? max) LengthIntervalFromModifiers(int? declaredMinLength, int? declaredMaxLength, bool notEmpty)
    {
        int min = declaredMinLength ?? (notEmpty ? 1 : 0);
        if (notEmpty)
            min = Math.Max(min, 1);
        return (min, declaredMaxLength);
    }

    /// <summary>
    /// Length intervals for the length-stable string functions (Functions catalog, String category):
    /// <c>trim(x)</c> → <c>(0, maxX)</c> (can strip everything); <c>toLower(x)</c>/<c>toUpper(x)</c>
    /// → <c>(minX, maxX)</c> (length-preserving); <c>left(x,n)</c>/<c>right(x,n)</c> with literal n
    /// → <c>(0, min(n, maxX))</c>; <c>mid(x,start,length)</c> with literal length → <c>(0, min(length, maxX))</c>.
    /// Any non-literal length argument or unrecognized function → unbounded.
    /// </summary>
    private static (int min, int? max) StringFunctionLengthInterval(TypedFunctionCall call, SemanticIndex semantics)
    {
        var args = call.Arguments;
        switch (call.ResolvedFunction)
        {
            case FunctionKind.Trim when args.Length >= 1:
            {
                var (_, max) = StringLengthIntervalOf(args[0], semantics);
                return (0, max); // trimming can remove every character
            }

            case FunctionKind.ToLower or FunctionKind.ToUpper when args.Length >= 1:
                return StringLengthIntervalOf(args[0], semantics); // length-preserving

            case FunctionKind.Left or FunctionKind.Right when args.Length >= 2:
            {
                var (_, srcMax) = StringLengthIntervalOf(args[0], semantics);
                return (0, CapByLiteralCount(args[1], srcMax));
            }

            case FunctionKind.Mid when args.Length >= 3:
            {
                var (_, srcMax) = StringLengthIntervalOf(args[0], semantics);
                // The result is at most `length` code units (3rd arg), clamped to the source length.
                return (0, CapByLiteralCount(args[2], srcMax));
            }

            default:
                return (0, null);
        }
    }

    /// <summary>
    /// Upper bound for a length-clamping function: the literal count argument capped by the source's
    /// own max length. Returns the source max when the count is not a non-negative integer literal.
    /// </summary>
    private static int? CapByLiteralCount(TypedExpression countArg, int? sourceMax)
    {
        if (TryExtractIntLiteral(countArg, out var n) && n >= 0)
            return sourceMax.HasValue ? Math.Min(n, sourceMax.Value) : n;
        return sourceMax; // non-literal count: bounded only by the source length (or unbounded)
    }

    private static bool TryExtractIntLiteral(TypedExpression expr, out int value)
    {
        if (expr is TypedLiteral literal)
        {
            switch (literal.Value)
            {
                case int i:
                    value = i;
                    return true;
                case long l when l >= int.MinValue && l <= int.MaxValue:
                    value = (int)l;
                    return true;
                case decimal d when decimal.Truncate(d) == d && d >= int.MinValue && d <= int.MaxValue:
                    value = (int)d;
                    return true;
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to prove a <see cref="CountContainmentProofRequirement"/> obligation.
    /// V1: always returns <c>null</c> (unresolved) because collection set assignments
    /// are rejected by the type checker, and add/remove actions do not yet generate obligations.
    /// </summary>
    internal static bool? TryCountContainmentProof(CountContainmentProofRequirement _, TypedExpression __)
        => null;
}
