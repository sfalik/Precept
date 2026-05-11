using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

// Partial-class file for PreceptTypeChecker — narrowing logic.
// Contains proof-context narrowing methods that refine type and interval information
// from guard expressions, assignments, and ensure clauses.
internal static partial class PreceptTypeChecker
{
    private static IReadOnlyDictionary<string, StaticValueKind> BuildEventEnsureSymbols(
        PreceptDefinition model,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName)
    {
        var symbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
        if (!eventArgKinds.TryGetValue(eventName, out var args))
            return symbols;

        foreach (var pair in args)
        {
            symbols[pair.Key] = pair.Value;
            symbols[$"{eventName}.{pair.Key}"] = pair.Value;
            if (HasFlag(pair.Value, StaticValueKind.String))
            {
                symbols[$"{pair.Key}.length"] = StaticValueKind.Number;
                symbols[$"{eventName}.{pair.Key}.length"] = StaticValueKind.Number;
            }
        }

        return symbols;
    }

    private static IReadOnlyDictionary<string, GlobalProofContext> BuildStateEnsureNarrowings(
        PreceptDefinition model,
        GlobalProofContext dataFieldKinds)
    {
        var result = new Dictionary<string, GlobalProofContext>(StringComparer.Ordinal);
        if (model.StateEnsures is null || model.StateEnsures.Count == 0)
            return result;

        foreach (var group in model.StateEnsures
            .Where(static stateEnsure => stateEnsure.Anchor == EnsureAnchor.In && stateEnsure.WhenGuard is null)
            .GroupBy(static stateEnsure => stateEnsure.State, StringComparer.Ordinal))
        {
            GlobalProofContext narrowed = new GlobalProofContext(
                new Dictionary<string, StaticValueKind>(dataFieldKinds.Symbols, StringComparer.Ordinal),
                new Dictionary<LinearForm, RelationalFact>(),
                CopyFieldIntervals(dataFieldKinds),
                CopyFlags(dataFieldKinds),
                CopyExprFacts(dataFieldKinds));

            foreach (var stateEnsure in group)
                narrowed = ApplyNarrowing(stateEnsure.Expression, narrowed, assumeTrue: true);

            result[group.Key] = narrowed;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, GlobalProofContext> BuildEventEnsureNarrowings(
        PreceptDefinition model,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds)
    {
        var result = new Dictionary<string, GlobalProofContext>(StringComparer.Ordinal);
        if (model.EventEnsures is null || model.EventEnsures.Count == 0)
            return result;

        foreach (var group in model.EventEnsures
            .Where(static e => e.WhenGuard is null)
            .GroupBy(static e => e.EventName, StringComparer.Ordinal))
        {
            var eventName = group.Key;
            if (!eventArgKinds.TryGetValue(eventName, out var args))
                continue;

            // Build bare-name symbol table for the event's args
            GlobalProofContext bareContext = new GlobalProofContext(new Dictionary<string, StaticValueKind>(args, StringComparer.Ordinal));

            foreach (var eventEnsure in group)
                bareContext = ApplyNarrowing(eventEnsure.Expression, bareContext, assumeTrue: true);

            // Translate typed stores from bare arg names to dotted form for transition-row scope.
            // Event ensures use bare arg names (e.g. "Days"), but transition rows use dotted names
            // (e.g. "Submit.Days").
            var dottedSymbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
            foreach (var pair in bareContext.Symbols)
                dottedSymbols[eventName + "." + pair.Key] = pair.Value;

            var dottedFieldIntervals = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
            foreach (var pair in bareContext.FieldIntervals)
                dottedFieldIntervals[eventName + "." + pair.Key] = pair.Value;

            var dottedFlags = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
            foreach (var pair in bareContext.Flags)
                dottedFlags[eventName + "." + pair.Key] = pair.Value;

            var dottedRelational = new Dictionary<LinearForm, RelationalFact>();
            foreach (var pair in bareContext.RelationalFacts)
                dottedRelational[pair.Key.Rekey(eventName)] = pair.Value;

            var dottedExprFacts = new Dictionary<LinearForm, NumericInterval>();
            foreach (var pair in bareContext.ExprFacts)
                dottedExprFacts[pair.Key.Rekey(eventName)] = pair.Value;

            result[eventName] = new GlobalProofContext(dottedSymbols, dottedRelational, dottedFieldIntervals, dottedFlags, dottedExprFacts);
        }

        return result;
    }

    /// <summary>
    /// Updates proof markers in <paramref name="symbols"/> after a <c>set <paramref name="targetField"/> = <paramref name="rhs"/></c>
    /// assignment. Called within assignment loops to thread post-mutation proof state into subsequent
    /// assignments in the same row or state action (Layer 1: Sequential Assignment Flow).
    /// </summary>
    internal static GlobalProofContext ApplyAssignmentNarrowing(
        string targetField,
        PreceptExpression rhs,
        GlobalProofContext context)
    {
        var markers = new Dictionary<string, StaticValueKind>(context.Symbols, StringComparer.Ordinal);
        var fieldIntervals = CopyFieldIntervals(context);
        var flags = CopyFlags(context);
        var exprFacts = CopyExprFacts(context);

        // Always kill existing numeric proof state for the target field first.
        flags.Remove(targetField);

        fieldIntervals.Remove(targetField);

        // Kill exprFacts entries where the LinearForm mentions the reassigned field.
        var exprKeysToKill = new List<LinearForm>();
        foreach (var lf in exprFacts.Keys)
        {
            if (lf.Terms.ContainsKey(targetField))
                exprKeysToKill.Add(lf);
        }
        foreach (var k in exprKeysToKill)
            exprFacts.Remove(k);

        rhs = StripParentheses(rhs);

        if (rhs is PreceptLiteralExpression lit)
        {
            double? val = lit.Value switch
            {
                long l => (double)l,
                double d => d,
                decimal m => (double)m,
                _ => null   // null literal — all numeric markers already killed above
            };

            if (val is double numVal)
            {
                if (numVal > 0)
                {
                    flags[targetField] = NumericFlags.Positive | NumericFlags.Nonnegative | NumericFlags.Nonzero;
                }
                else if (numVal == 0.0)
                {
                    flags[targetField] = NumericFlags.Nonnegative;
                }
                else // negative literal — nonzero but not positive/nonneg
                {
                    flags[targetField] = NumericFlags.Nonzero;
                }

                // Also inject a point interval for precise interval arithmetic in subsequent expressions.
                var pointIval = new NumericInterval(numVal, true, numVal, true);
                fieldIntervals[targetField] = pointIval;
            }
        }
        else if (TryGetIdentifierKey(rhs, out var sourceKey))
        {
            // Copy numeric proof state from source identifier to target field.
            if (context.Flags.TryGetValue(sourceKey, out var srcFlags))
                flags[targetField] = srcFlags;

            // Copy interval from source to target via typed store.
            if (context.FieldIntervals.TryGetValue(sourceKey, out var srcIval))
            {
                fieldIntervals[targetField] = srcIval;
            }
        }
        else
        {
            // Compound RHS: derive interval from proof engine and inject sign markers.
            var rhsInterval = context.IntervalOf(rhs).Interval;
            if (!rhsInterval.IsUnknown)
            {
                fieldIntervals[targetField] = rhsInterval;
                if (rhsInterval.IsPositive)
                {
                    flags[targetField] = NumericFlags.Positive | NumericFlags.Nonnegative | NumericFlags.Nonzero;
                }
                else if (rhsInterval.IsNonnegative)
                {
                    flags[targetField] = NumericFlags.Nonnegative;
                }
                else if (rhsInterval.ExcludesZero)
                {
                    flags[targetField] = NumericFlags.Nonzero;
                }

                // Dual-write: store compound RHS interval in exprFacts (step 7b-iii).
                var rhsForm = LinearForm.TryNormalize(rhs);
                if (rhsForm is not null)
                {
                    exprFacts[rhsForm] = rhsInterval;
                    var targetForm = LinearForm.FromField(targetField);
                    exprFacts[targetForm.Subtract(rhsForm)] = new NumericInterval(0, true, 0, true);
                }
            }
        }

        // Preserve relational facts, killing any that mention the reassigned field.
        var relFacts = new Dictionary<LinearForm, RelationalFact>();
        foreach (var (lf, fact) in context.RelationalFacts)
        {
            if (!lf.Terms.ContainsKey(targetField))
                relFacts[lf] = fact;
        }

        return new GlobalProofContext(markers, relFacts, fieldIntervals, flags, exprFacts);
    }

    internal static GlobalProofContext ApplyNarrowing(
        PreceptExpression expression,
        GlobalProofContext context,
        bool assumeTrue)
    {
        expression = StripParentheses(expression);

        if (expression is PreceptUnaryExpression { Operator: "not" } unary)
            return ApplyNarrowing(unary.Operand, context, !assumeTrue);

        if (expression is not PreceptBinaryExpression binary)
            return context;

        if (binary.Operator == "and")
        {
            if (!assumeTrue)
                return context;

            var leftNarrowed = ApplyNarrowing(binary.Left, context, assumeTrue: true);
            return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: true);
        }

        if (binary.Operator == "or")
        {
            if (assumeTrue)
            {
                // SOUNDNESS: This proof is sound ONLY because C42 independently prevents null fields
                // from reaching arithmetic. If C42 is ever relaxed for nullable arithmetic, this
                // decomposition becomes unsound.
                if (TryDecomposeNullOrPattern(binary, context, out var decomposed))
                    return decomposed;
                return context;
            }

            var leftNarrowed = ApplyNarrowing(binary.Left, context, assumeTrue: false);
            return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: false);
        }

        if (binary.Operator is "==" or "!=" &&
            TryApplyNullComparisonNarrowing(binary, context, assumeTrue, out var nullNarrowed))
            return nullNarrowed;

        if (TryApplyNumericComparisonNarrowing(binary, context, assumeTrue, out var numericNarrowed))
            return numericNarrowed;

        return context;
    }

    private static bool TryApplyNullComparisonNarrowing(
        PreceptBinaryExpression binary,
        GlobalProofContext context,
        bool assumeTrue,
        out GlobalProofContext narrowed)
    {
        narrowed = context;

        var leftIsNull = IsNullLiteral(binary.Left);
        var rightIsNull = IsNullLiteral(binary.Right);
        if (!leftIsNull && !rightIsNull)
            return false;

        string key;
        if (leftIsNull)
        {
            if (!TryGetIdentifierKey(binary.Right, out key))
                return false;
        }
        else
        {
            if (!TryGetIdentifierKey(binary.Left, out key))
                return false;
        }

        if (!context.Symbols.TryGetValue(key, out var existingKind))
            return false;

        var expectsNull = binary.Operator switch
        {
            "==" => assumeTrue,
            "!=" => !assumeTrue,
            _ => false
        };

        var updatedKind = expectsNull
            ? StaticValueKind.Null
            : (existingKind & ~StaticValueKind.Null);

        narrowed = new GlobalProofContext(
            new Dictionary<string, StaticValueKind>(context.Symbols, StringComparer.Ordinal)
            {
                [key] = updatedKind
            },
            new Dictionary<LinearForm, RelationalFact>(),
            CopyFieldIntervals(context),
            CopyFlags(context),
            CopyExprFacts(context));
        return true;
    }

    private static bool TryApplyNumericComparisonNarrowing(
        PreceptBinaryExpression binary,
        GlobalProofContext context,
        bool assumeTrue,
        out GlobalProofContext result)
    {
        result = context;

        if (!assumeTrue)
        {
            // Negate the comparison: not(A > B) ≡ A <= B, etc.
            var negatedOp = binary.Operator switch
            {
                ">" => "<=",
                ">=" => "<",
                "<" => ">=",
                "<=" => ">",
                "!=" => "==",
                "==" => "!=",
                _ => (string?)null
            };
            if (negatedOp is null) return false;
            var negated = new PreceptBinaryExpression(negatedOp, binary.Left, binary.Right);
            return TryApplyNumericComparisonNarrowing(negated, context, assumeTrue: true, out result);
        }

        if (binary.Operator is not (">" or ">=" or "<" or "<=" or "!=" or "=="))
            return false;

        bool leftIsId = TryGetIdentifierKey(binary.Left, out var leftKey);
        bool rightIsId = TryGetIdentifierKey(binary.Right, out var rightKey);
        bool leftIsLit = TryGetNumericLiteral(binary.Left, out var leftVal);
        bool rightIsLit = TryGetNumericLiteral(binary.Right, out var rightVal);

        string key;
        double lit;
        string op;

        if (leftIsId && rightIsLit)
        {
            key = leftKey;
            lit = rightVal;
            op = binary.Operator;
        }
        else if (rightIsId && leftIsLit)
        {
            key = rightKey;
            lit = leftVal;
            op = FlipComparisonOperator(binary.Operator);
        }
        else if (leftIsId && rightIsId)
        {
            var relMarkers = new Dictionary<string, StaticValueKind>(context.Symbols, StringComparer.Ordinal);
            var relFacts = CopyRelationalFacts(context);
            switch (binary.Operator)
            {
                case ">":
                    TryStoreLinearFact(binary.Left, binary.Right, RelationKind.GreaterThan, relFacts);
                    break;
                case ">=":
                    TryStoreLinearFact(binary.Left, binary.Right, RelationKind.GreaterThanOrEqual, relFacts);
                    break;
                case "<":  // B < A → A > B (canonicalized)
                    TryStoreLinearFact(binary.Right, binary.Left, RelationKind.GreaterThan, relFacts);
                    break;
                case "<=": // B <= A → A >= B (canonicalized)
                    TryStoreLinearFact(binary.Right, binary.Left, RelationKind.GreaterThanOrEqual, relFacts);
                    break;
                default:
                    return false;
            }
            result = new GlobalProofContext(relMarkers, relFacts, CopyFieldIntervals(context), CopyFlags(context), CopyExprFacts(context));
            return true;
        }
        else if (binary.Operator is ">" or ">=" or "<" or "<=")
        {
            // General case: compound expressions on one or both sides.
            // Try to normalize both sides. If both normalize, store a LinearForm-keyed fact (closes Gap 2).
            var leftForm  = LinearForm.TryNormalize(binary.Left);
            var rightForm = LinearForm.TryNormalize(binary.Right);
            if (leftForm is null || rightForm is null)
                return false;

            var relFacts = CopyRelationalFacts(context);
            switch (binary.Operator)
            {
                case ">":
                    relFacts[GlobalProofContext.GcdNormalize(leftForm.Subtract(rightForm))] = new RelationalFact(RelationKind.GreaterThan);
                    break;
                case ">=":
                    relFacts[GlobalProofContext.GcdNormalize(leftForm.Subtract(rightForm))] = new RelationalFact(RelationKind.GreaterThanOrEqual);
                    break;
                case "<":
                    relFacts[GlobalProofContext.GcdNormalize(rightForm.Subtract(leftForm))] = new RelationalFact(RelationKind.GreaterThan);
                    break;
                case "<=":
                    relFacts[GlobalProofContext.GcdNormalize(rightForm.Subtract(leftForm))] = new RelationalFact(RelationKind.GreaterThanOrEqual);
                    break;
                default:
                    return false;
            }
            result = new GlobalProofContext(new Dictionary<string, StaticValueKind>(context.Symbols, StringComparer.Ordinal), relFacts, CopyFieldIntervals(context), CopyFlags(context), CopyExprFacts(context));
            return true;
        }
        else
        {
            return false;
        }

        // Canonicalized: key <op> lit
        var markers = new Dictionary<string, StaticValueKind>(context.Symbols, StringComparer.Ordinal);
        var flags = CopyFlags(context);
        bool injected = false;

        if (op == ">" && lit >= 0)
        {
            flags[key] = flags.GetValueOrDefault(key) | NumericFlags.Positive | NumericFlags.Nonnegative | NumericFlags.Nonzero;
            injected = true;
        }
        else if (op == ">=" && lit > 0)
        {
            flags[key] = flags.GetValueOrDefault(key) | NumericFlags.Positive | NumericFlags.Nonnegative | NumericFlags.Nonzero;
            injected = true;
        }
        else if (op == ">=" && lit == 0)
        {
            flags[key] = flags.GetValueOrDefault(key) | NumericFlags.Nonnegative;
            injected = true;
        }
        else if (op == "!=" && lit == 0)
        {
            flags[key] = flags.GetValueOrDefault(key) | NumericFlags.Nonzero;
            injected = true;
        }
        else if (op == "<" && lit <= 0)
        {
            flags[key] = flags.GetValueOrDefault(key) | NumericFlags.Nonzero;
            injected = true;
        }

        if (!injected)
            return false;

        result = new GlobalProofContext(markers, CopyRelationalFacts(context), CopyFieldIntervals(context), flags, CopyExprFacts(context));
        return true;
    }

    /// <summary>
    /// Attempts to normalize <paramref name="lhs"/> and <paramref name="rhs"/> to
    /// <see cref="LinearForm"/> and store <c>lhs - rhs &gt; 0</c> (or <c>&gt;= 0</c>)
    /// as a typed <see cref="RelationalFact"/> in <paramref name="relFacts"/>.
    /// No-op when either side is non-normalizable.
    /// </summary>
    private static void TryStoreLinearFact(
        PreceptExpression lhs,
        PreceptExpression rhs,
        RelationKind kind,
        Dictionary<LinearForm, RelationalFact> relFacts)
    {
        var lf = LinearForm.TryNormalize(lhs);
        var rf = LinearForm.TryNormalize(rhs);
        if (lf is not null && rf is not null)
            relFacts[GlobalProofContext.GcdNormalize(lf.Subtract(rf))] = new RelationalFact(kind);
    }

    /// <summary>
    /// Recognizes <c>Field == null or Field &gt; 0</c> (or reversed ordering) produced by MaybeNullGuard
    /// for nullable fields with numeric constraints, and extracts the numeric proof from the non-null branch.
    /// </summary>
    private static bool TryDecomposeNullOrPattern(
        PreceptBinaryExpression binary,
        GlobalProofContext context,
        out GlobalProofContext result)
    {
        result = context;

        if (binary.Operator != "or")
            return false;

        // Try both orderings: left=null-check + right=numeric, or left=numeric + right=null-check
        var left = StripParentheses(binary.Left);
        var right = StripParentheses(binary.Right);

        if (TryDecomposeOrdered(left, right, context, out result))
            return true;
        if (TryDecomposeOrdered(right, left, context, out result))
            return true;

        return false;

        static bool TryDecomposeOrdered(
            PreceptExpression nullCandidate,
            PreceptExpression numericCandidate,
            GlobalProofContext syms,
            out GlobalProofContext res)
        {
            res = syms;

            // Null-check branch: Field == null or null == Field
            if (nullCandidate is not PreceptBinaryExpression { Operator: "==" } nullBinary)
                return false;

            bool leftIsNull = IsNullLiteral(nullBinary.Left);
            bool rightIsNull = IsNullLiteral(nullBinary.Right);
            if (!leftIsNull && !rightIsNull)
                return false;

            // Both sides are null → no numeric proof to extract
            if (leftIsNull && rightIsNull)
                return false;

            var nullSideIdExpr = leftIsNull ? nullBinary.Right : nullBinary.Left;
            if (!TryGetIdentifierKey(nullSideIdExpr, out var nullFieldKey))
                return false;

            // Numeric branch — could be a direct comparison or a compound 'and'
            var stripped = StripParentheses(numericCandidate);

            if (stripped is PreceptBinaryExpression { Operator: "and" } andBinary)
            {
                // Compound: Field == null or (Field >= 0 and Field < 100)
                // Recurse into each 'and' operand to accumulate markers
                GlobalProofContext accumulated = syms;
                bool anyInjected = false;

                foreach (var operand in new[] { andBinary.Left, andBinary.Right })
                {
                    // Verify same-field identity
                    if (!TryGetNumericBranchFieldKey(operand, out var andKey) || andKey != nullFieldKey)
                        continue;

                    var narrowed = ApplyNarrowing(operand, accumulated, assumeTrue: true);
                    if (!ReferenceEquals(narrowed, accumulated))
                    {
                        accumulated = narrowed;
                        anyInjected = true;
                    }
                }

                if (!anyInjected)
                    return false;

                res = accumulated;
                return true;
            }

            // Direct comparison: Field == null or Field > 0
            if (!TryGetNumericBranchFieldKey(stripped, out var numericFieldKey))
                return false;

            // Same-field identity check: prevent unsound cross-field decomposition
            if (numericFieldKey != nullFieldKey)
                return false;

            var result = ApplyNarrowing(stripped, syms, assumeTrue: true);
            if (ReferenceEquals(result, syms))
                return false;

            res = result;
            return true;
        }

        static bool TryGetNumericBranchFieldKey(PreceptExpression expr, out string key)
        {
            key = string.Empty;
            var stripped = StripParentheses(expr);
            if (stripped is not PreceptBinaryExpression cmp)
                return false;

            if (TryGetIdentifierKey(cmp.Left, out key))
                return true;
            if (TryGetIdentifierKey(cmp.Right, out key))
                return true;

            return false;
        }
    }
}
