using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    //  S10 — Constraint Influence Analysis
    // ════════════════════════════════════════════════════════════════════════════

    private static ImmutableArray<ConstraintInfluenceEntry> ProjectConstraintInfluence(SemanticIndex semantics)
    {
        var entries = new List<ConstraintInfluenceEntry>();

        foreach (var cfr in semantics.ConstraintRefs)
        {
            var qualifiedArgs = cfr.ReferencedArgs
                .Select(argName => ResolveArgToEvent(argName, semantics))
                .ToImmutableArray();

            entries.Add(new ConstraintInfluenceEntry(
                cfr.ConstraintIdentity,
                cfr.ReferencedFields,
                qualifiedArgs));
        }

        return entries.ToImmutableArray();
    }

    private static EventArgReference ResolveArgToEvent(string argName, SemanticIndex semantics)
    {
        foreach (var evt in semantics.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (string.Equals(arg.Name, argName, StringComparison.Ordinal))
                    return new EventArgReference(evt.Name, argName);
            }
        }
        return new EventArgReference("<unknown>", argName);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S11 — Initial-State Satisfiability
    // ════════════════════════════════════════════════════════════════════════════

    private static ImmutableArray<InitialStateSatisfiabilityResult> CheckInitialStateSatisfiability(
        SemanticIndex semantics)
    {
        var initialState = semantics.States
            .FirstOrDefault(s => s.Modifiers.Contains(ModifierKind.InitialState));
        if (initialState is null)
            return ImmutableArray<InitialStateSatisfiabilityResult>.Empty;

        // Build default value environment
        var defaults = new Dictionary<string, object?>(StringComparer.Ordinal);
        var unfoldable = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in semantics.Fields)
        {
            if (field.DefaultExpression is TypedLiteral lit)
                defaults[field.Name] = lit.Value;
            else if (field.DefaultExpression is not null || field.IsComputed)
                unfoldable.Add(field.Name);
            else if (field.IsOptional)
                defaults[field.Name] = null;
            else
                defaults[field.Name] = GetTypeDefault(field.ResolvedType, unfoldable, field.Name);
        }

        // Collect StateResident ensures for initial state
        var initialEnsures = semantics.EnsuresByState.TryGetValue(initialState.Name, out var ensures)
            ? ensures.Where(e => e.Kind == ConstraintKind.StateResident).ToList()
            : new List<TypedEnsure>();

        var violations = new List<UnsatisfiedConstraint>();

        for (int i = 0; i < initialEnsures.Count; i++)
        {
            var ensure = initialEnsures[i];
            if (ensure.Guard is not null)
                continue; // guarded ensures skipped

            var foldResult = ConstantFold(ensure.Condition, defaults, unfoldable);

            if (foldResult is false)
            {
                violations.Add(new UnsatisfiedConstraint(
                    new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, i),
                    FormatViolationReason(ensure, defaults)));
            }
        }

        return
        [
            new InitialStateSatisfiabilityResult(
                initialState.Name,
                violations.Count == 0,
                violations.ToImmutableArray())
        ];
    }

    private static object? GetTypeDefault(TypeKind type, HashSet<string> unfoldable, string fieldName)
    {
        return type switch
        {
            TypeKind.Integer => 0m,
            TypeKind.Decimal => 0m,
            TypeKind.Number => 0m,
            TypeKind.String => "",
            TypeKind.Boolean => false,
            TypeKind.Set or TypeKind.Queue or TypeKind.Stack or TypeKind.Log or
            TypeKind.LogBy or TypeKind.Bag or TypeKind.List or TypeKind.QueueBy or
            TypeKind.Lookup => 0m, // collection count = 0
            _ => MarkUnfoldable(unfoldable, fieldName)
        };
    }

    private static object? MarkUnfoldable(HashSet<string> unfoldable, string fieldName)
    {
        unfoldable.Add(fieldName);
        return null;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Test entry points (InternalsVisibleTo — Precept.Tests)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Exposes <see cref="ExtractComparableValue"/> for unit testing.</summary>
    internal static string? ExtractComparableValueForTest(DeclaredQualifierMeta qualifier) =>
        ExtractComparableValue(qualifier);

    /// <summary>Exposes qualifier compatibility comparisons for unit testing.</summary>
    internal static bool QualifiersAreCompatibleForTest(
        DeclaredQualifierMeta? leftQualifier,
        DeclaredQualifierMeta? rightQualifier,
        QualifierAxis axis) =>
        QualifiersAreCompatible(leftQualifier, rightQualifier, axis);

    /// <summary>Exposes <see cref="QualifiersSymbolicallyEqual"/> for unit testing.</summary>
    internal static bool QualifiersSymbolicallyEqualForTest(
        DeclaredQualifierMeta left,
        DeclaredQualifierMeta right) =>
        QualifiersSymbolicallyEqual(left, right);

    /// <summary>
    /// Returns the first implied qualifier matching <paramref name="axis"/> for <paramref name="type"/>.
    /// Tests Slice 11B's Duration implied-qualifier path without requiring proof pipeline setup.
    /// </summary>
    internal static DeclaredQualifierMeta? GetImpliedQualifierOnAxis(TypeKind type, QualifierAxis axis)
    {
        var typeMeta = Types.GetMeta(type);
        foreach (var qual in typeMeta.ImpliedQualifiers)
        {
            if (qual.Axis == axis)
                return qual;
        }
        return null;
    }

    private static bool? ConstantFold(TypedExpression expr, Dictionary<string, object?> defaults, HashSet<string> unfoldable)
    {
        var result = FoldValue(expr, defaults, unfoldable);
        return result switch
        {
            bool b => b,
            _ => null // Unknown
        };
    }

    private static object? FoldValue(TypedExpression expr, Dictionary<string, object?> defaults, HashSet<string> unfoldable)
    {
        switch (expr)
        {
            case TypedLiteral lit:
                return lit.Value;

            case TypedFieldRef fieldRef:
                if (unfoldable.Contains(fieldRef.FieldName))
                    return UnknownSentinel;
                return defaults.TryGetValue(fieldRef.FieldName, out var val) ? val : UnknownSentinel;

            case TypedBinaryOp bin:
            {
                var left = FoldValue(bin.Left, defaults, unfoldable);
                var right = FoldValue(bin.Right, defaults, unfoldable);
                if (ReferenceEquals(left, UnknownSentinel) || ReferenceEquals(right, UnknownSentinel))
                    return UnknownSentinel;

                var opKind = Operations.GetMeta(bin.ResolvedOp).Op;
                return EvaluateBinaryOp(opKind, left, right);
            }

            case TypedUnaryOp un:
            {
                var operand = FoldValue(un.Operand, defaults, unfoldable);
                if (ReferenceEquals(operand, UnknownSentinel))
                    return UnknownSentinel;

                var opKind = Operations.GetMeta(un.ResolvedOp).Op;
                if (opKind == OperatorKind.Not && operand is bool b)
                    return !b;
                if (opKind == OperatorKind.Negate && operand is decimal d)
                    return -d;
                return UnknownSentinel;
            }

            case TypedConditional cond:
            {
                var condResult = FoldValue(cond.Condition, defaults, unfoldable);
                if (condResult is bool condBool)
                    return condBool
                        ? FoldValue(cond.ThenBranch, defaults, unfoldable)
                        : FoldValue(cond.ElseBranch, defaults, unfoldable);
                return UnknownSentinel;
            }

            case TypedPostfixOp post:
            {
                var operand = FoldValue(post.Operand, defaults, unfoldable);
                if (ReferenceEquals(operand, UnknownSentinel))
                    return UnknownSentinel;
                bool isSet = operand is not null;
                return post.IsNegated ? !isSet : isSet;
            }

            default:
                return UnknownSentinel;
        }
    }

    private static readonly object UnknownSentinel = new();

    private readonly record struct NumericRuleFact(string FieldName, OperatorKind Comparison, decimal Value);

    private static object? EvaluateBinaryOp(OperatorKind op, object? left, object? right)
    {
        // Boolean operations
        if (op == OperatorKind.And && left is bool lb1 && right is bool rb1)
            return lb1 && rb1;
        if (op == OperatorKind.Or && left is bool lb2 && right is bool rb2)
            return lb2 || rb2;

        // Numeric comparisons
        var dl = left switch { decimal d => (decimal?)d, int i => (decimal?)i, long l => (decimal?)l, _ => null };
        var dr = right switch { decimal d => (decimal?)d, int i => (decimal?)i, long l => (decimal?)l, _ => null };

        if (dl is not null && dr is not null)
        {
            return op switch
            {
                OperatorKind.Plus => dl.Value + dr.Value,
                OperatorKind.Minus => dl.Value - dr.Value,
                OperatorKind.Times => dl.Value * dr.Value,
                OperatorKind.Divide when dr.Value != 0 => dl.Value / dr.Value,
                OperatorKind.Modulo when dr.Value != 0 => dl.Value % dr.Value,
                OperatorKind.Equals => dl.Value == dr.Value,
                OperatorKind.NotEquals => dl.Value != dr.Value,
                OperatorKind.GreaterThan => dl.Value > dr.Value,
                OperatorKind.GreaterThanOrEqual => dl.Value >= dr.Value,
                OperatorKind.LessThan => dl.Value < dr.Value,
                OperatorKind.LessThanOrEqual => dl.Value <= dr.Value,
                _ => UnknownSentinel
            };
        }

        // String equality
        if (left is string sl && right is string sr)
        {
            return op switch
            {
                OperatorKind.Equals => string.Equals(sl, sr, StringComparison.Ordinal),
                OperatorKind.NotEquals => !string.Equals(sl, sr, StringComparison.Ordinal),
                _ => UnknownSentinel
            };
        }

        // Boolean equality
        if (left is bool bl && right is bool br)
        {
            return op switch
            {
                OperatorKind.Equals => bl == br,
                OperatorKind.NotEquals => bl != br,
                _ => UnknownSentinel
            };
        }

        return UnknownSentinel;
    }

    private static string FormatViolationReason(TypedEnsure ensure, Dictionary<string, object?> defaults)
    {
        var fields = new List<string>();
        CollectFieldRefs(ensure.Condition, fields);
        if (fields.Count == 0)
            return "constraint fails with default values";

        var details = fields.Select(f =>
            defaults.TryGetValue(f, out var v) ? $"{f}={v ?? "null"}" : f);
        return $"constraint fails when {string.Join(", ", details)}";
    }

    private static void CollectFieldRefs(TypedExpression expr, List<string> fields)
    {
        switch (expr)
        {
            case TypedFieldRef fr:
                if (!fields.Contains(fr.FieldName)) fields.Add(fr.FieldName);
                break;
            case TypedBinaryOp bin:
                CollectFieldRefs(bin.Left, fields);
                CollectFieldRefs(bin.Right, fields);
                break;
            case TypedUnaryOp un:
                CollectFieldRefs(un.Operand, fields);
                break;
            case TypedConditional cond:
                CollectFieldRefs(cond.Condition, fields);
                CollectFieldRefs(cond.ThenBranch, fields);
                CollectFieldRefs(cond.ElseBranch, fields);
                break;
            case TypedFunctionCall call:
                foreach (var arg in call.Arguments) CollectFieldRefs(arg, fields);
                break;
            case TypedMemberAccess ma:
                CollectFieldRefs(ma.Object, fields);
                break;
        }
    }
}
