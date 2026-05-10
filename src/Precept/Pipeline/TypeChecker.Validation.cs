using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using NodaTime;
using NodaTime.Text;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    // ════════════════════════════════════════════════════════════════════════
    //  Modifier and structural validation
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Validate modifier applicability, conflicts, and subsumption for all fields and states.</summary>
    private static void ValidateModifiers(CheckContext ctx)
    {
        foreach (var field in ctx.Fields)
        {
            if (field.ResolvedType == TypeKind.Error) continue;
            ValidateValueModifiers(
                field.Syntax.GetSlot<ModifierListSlot>(ConstructSlotKind.ModifierList)?.Modifiers ?? ImmutableArray<ParsedModifier>.Empty,
                field.ResolvedType,
                field.ImpliedModifiers,
                field.IsComputed,
                field.Syntax.Span,
                field.Name,
                isEventArg: false,
                ctx);
        }

        foreach (var evt in ctx.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.ResolvedType == TypeKind.Error) continue;
                ValidateValueModifiers(
                    arg.Modifiers.Select(kind => new ParsedModifier(kind, null)).ToImmutableArray(),
                    arg.ResolvedType,
                    ImmutableArray<ModifierKind>.Empty,
                    isComputed: false,
                    arg.Span,
                    arg.Name,
                    isEventArg: true,
                    ctx);
            }
        }
    }

    /// <summary>
    /// Catalog-driven modifier validation for a single field or event arg declaration.
    /// Checks applicability, duplicates, mutual exclusivity, subsumption, redundancy with
    /// implied modifiers, and writable restrictions.
    /// </summary>
    private static void ValidateValueModifiers(
        ImmutableArray<ParsedModifier> modifiers,
        TypeKind resolvedType,
        ImmutableArray<ModifierKind> impliedModifiers,
        bool isComputed,
        SourceSpan span,
        string declarationName,
        bool isEventArg,
        CheckContext ctx)
    {
        var seen = new HashSet<ModifierKind>();

        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            var kind = modifier.Kind;
            var meta = Modifiers.GetMeta(kind);

            // Duplicate check
            if (!seen.Add(kind))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.DuplicateModifier, span, meta.Token.Text));
                continue;
            }

            // Only ValueModifierMeta modifiers are valid on fields/args
            if (meta is not ValueModifierMeta valueMeta)
                continue;

            // Applicability: empty ApplicableTo = any type; otherwise check membership
            if (valueMeta.ApplicableTo.Length > 0 &&
                !IsTypeApplicable(valueMeta.ApplicableTo, resolvedType, modifiers.Select(m => m.Kind).ToImmutableArray()))
            {
                var typeName = Types.GetMeta(resolvedType).DisplayName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.InvalidModifierForType, span, meta.Token.Text, typeName));
            }

            // Mutual exclusivity
            foreach (var conflict in meta.MutuallyExclusiveWith)
            {
                if (seen.Contains(conflict))
                {
                    var conflictMeta = Modifiers.GetMeta(conflict);
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.InvalidModifierForType, span,
                            meta.Token.Text, $"it conflicts with '{conflictMeta.Token.Text}'"));
                }
            }

            // Subsumption: if another explicit modifier already subsumes this one
            foreach (var other in seen)
            {
                if (other == kind) continue;
                var otherMeta = Modifiers.GetMeta(other);
                if (otherMeta is ValueModifierMeta otherValue && otherValue.Subsumes.Contains(kind))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.RedundantModifier, span,
                            meta.Token.Text, otherMeta.Token.Text));
                }
            }

            // Redundancy with implied modifiers (type already implies this modifier)
            if (impliedModifiers.Contains(kind))
            {
                var typeName = Types.GetMeta(resolvedType).DisplayName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.RedundantModifier, span,
                        meta.Token.Text, typeName));
            }

            // Writable on event arg
            var declarationSite = isEventArg
                ? ValueModifierDeclarationSite.EventArgDeclaration
                : ValueModifierDeclarationSite.FieldDeclaration;
            if (!valueMeta.ApplicableDeclarationSites.HasFlag(declarationSite))
            {
                if (isEventArg && kind == ModifierKind.Writable)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.WritableOnEventArg, span, declarationName));
                }
            }

            // Writable on computed field
            if (kind == ModifierKind.Writable && isComputed)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.ComputedFieldNotWritable, span, declarationName));
            }
        }

        ValidateModifierBounds(modifiers, span, ctx);
    }

    private static void ValidateModifierBounds(
        ImmutableArray<ParsedModifier> modifiers,
        SourceSpan span,
        CheckContext ctx)
    {
        if (modifiers.IsDefaultOrEmpty)
            return;

        var byKind = modifiers
            .GroupBy(modifier => modifier.Kind)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var modifier in modifiers)
        {
            var meta = Modifiers.GetMeta(modifier.Kind) as ValueModifierMeta;
            if (meta?.BoundCounterpart is null || !IsLowerBound(meta))
                continue;

            if (!byKind.TryGetValue(meta.BoundCounterpart.Value, out var counterpart))
                continue;

            var lowerValue = TryGetComparableModifierValue(modifier.Value);
            var upperValue = TryGetComparableModifierValue(counterpart.Value);
            if (lowerValue is null || upperValue is null || lowerValue <= upperValue)
                continue;

            var counterpartMeta = (ValueModifierMeta)Modifiers.GetMeta(meta.BoundCounterpart.Value);
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InvalidModifierBounds,
                    span,
                    meta.Token.Text ?? modifier.Kind.ToString(),
                    lowerValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    counterpartMeta.Token.Text ?? counterpart.Kind.ToString(),
                    upperValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    private static bool IsLowerBound(ValueModifierMeta meta)
        => meta.ProofSatisfactions
            .OfType<ProofSatisfaction.Numeric>()
            .Select(proof => proof.Comparison)
            .FirstOrDefault() is OperatorKind.GreaterThan or OperatorKind.GreaterThanOrEqual;

    private static decimal? TryGetComparableModifierValue(ParsedExpression? expr) => expr switch
    {
        LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var text }
            when decimal.TryParse(text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var value)
            => value,
        UnaryOperationExpression
        {
            Operator: TokenKind.Minus,
            Operand: LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var text }
        }
            when decimal.TryParse(text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var value)
            => -value,
        _ => null,
    };

    /// <summary>
    /// Check whether a resolved type matches any entry in a modifier's ApplicableTo array.
    /// Handles both simple <see cref="TypeTarget"/> and <see cref="ModifiedTypeTarget"/> entries.
    /// </summary>
    private static bool IsTypeApplicable(TypeTarget[] applicableTo, TypeKind resolvedType, ImmutableArray<ModifierKind> modifiers)
    {
        foreach (var target in applicableTo)
        {
            // Kind == null means "any type" within the target
            if (target.Kind is null || target.Kind == resolvedType)
            {
                if (target is ModifiedTypeTarget modified)
                {
                    // All required modifiers must be present
                    if (modified.RequiredModifiers.All(m => modifiers.Contains(m)))
                        return true;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Structural validation sub-pass: computed-field cycle detection and
    /// forward-reference belt-and-suspenders validation.
    /// Reads <see cref="CheckContext.ComputedDeps"/> (populated during computed expression
    /// resolution) and <see cref="CheckContext.Fields"/>.
    /// </summary>
    private static void ValidateStructural(CheckContext ctx)
    {
        // ── Computed field cycle detection (DFS) ──────────────────────────
        // Build adjacency list from ComputedDeps: fieldName → set of dependent field names.
        // O(n) construction, O(n) DFS traversal.
        if (ctx.ComputedDeps.Count > 0)
        {
            var adjacency = new Dictionary<string, List<string>>(ctx.ComputedDeps.Count);
            foreach (var dep in ctx.ComputedDeps)
            {
                if (!adjacency.TryGetValue(dep.FieldName, out var deps))
                {
                    deps = [];
                    adjacency[dep.FieldName] = deps;
                }
                deps.AddRange(dep.DependsOn);
            }

            // DFS with three-color marking: white (unvisited), gray (in stack), black (done)
            var white = new HashSet<string>(adjacency.Keys);
            var gray = new HashSet<string>();
            var black = new HashSet<string>();

            foreach (var startNode in adjacency.Keys)
            {
                if (!white.Contains(startNode)) continue;
                DetectCycles(startNode, adjacency, white, gray, black, [], ctx);
            }
        }

        // ── Forward-reference belt-and-suspenders ─────────────────────────
        // Verify computed field deps don't reference fields declared after the computed field.
        // This is a redundant check — ResolveIdentifier already enforces D8 at expression
        // resolution time. This pass catches any gap if expression resolution was bypassed.
        if (ctx.ComputedDeps.Count > 0)
        {
            var fieldIndex = new Dictionary<string, int>(ctx.Fields.Count);
            for (int i = 0; i < ctx.Fields.Count; i++)
                fieldIndex[ctx.Fields[i].Name] = i;

            foreach (var dep in ctx.ComputedDeps)
            {
                if (!fieldIndex.TryGetValue(dep.FieldName, out var sourceIdx)) continue;

                foreach (var target in dep.DependsOn)
                {
                    if (fieldIndex.TryGetValue(target, out var targetIdx) && targetIdx >= sourceIdx)
                    {
                        // Find the field's syntax span for the diagnostic
                        var field = ctx.FieldLookup[dep.FieldName];
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.DefaultForwardReference, field.Syntax.Span,
                                dep.FieldName, target));
                    }
                }
            }
        }
    }

    /// <summary>
    /// DFS cycle detection helper. Walks the adjacency graph using three-color marking.
    /// On back-edge detection (gray → gray), emits <see cref="DiagnosticCode.CircularComputedField"/>.
    /// </summary>
    private static void DetectCycles(
        string node,
        Dictionary<string, List<string>> adjacency,
        HashSet<string> white,
        HashSet<string> gray,
        HashSet<string> black,
        List<string> path,
        CheckContext ctx)
    {
        white.Remove(node);
        gray.Add(node);
        path.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (gray.Contains(neighbor))
                {
                    // Back edge → cycle. Build cycle description from path.
                    int cycleStart = path.IndexOf(neighbor);
                    var cycle = string.Join(" → ", path.Skip(cycleStart)) + " → " + neighbor;
                    var field = ctx.FieldLookup[neighbor];
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CircularComputedField, field.Syntax.Span,
                            neighbor, cycle));
                }
                else if (white.Contains(neighbor))
                {
                    DetectCycles(neighbor, adjacency, white, gray, black, path, ctx);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        gray.Remove(node);
        black.Add(node);
    }

    /// <summary>
    /// CI enforcement sub-pass (Slice 8): validate <c>~string</c> usage consistency.
    /// Walks all resolved expression trees and checks the 5 CI enforcement rules
    /// from the language spec §3.8. Rules 1–2 fire on <c>==</c> / <c>!=</c> with a
    /// <c>~string</c> operand. Rules 3 fires on <c>contains</c> with a <c>~string</c>
    /// value in a case-sensitive collection. Rules 4–5 fire on <c>startsWith</c> /
    /// <c>endsWith</c> with a <c>~string</c> first argument.
    /// </summary>
    private static void ValidateCIEnforcement(CheckContext ctx)
    {
        // Collect all root expression trees from the context
        foreach (var field in ctx.Fields)
        {
            if (field.DefaultExpression is not null)
                EnforceCIInExpression(field.DefaultExpression, ctx);
            if (field.ComputedExpression is not null)
                EnforceCIInExpression(field.ComputedExpression, ctx);
        }

        foreach (var row in ctx.TransitionRows)
        {
            if (row.Guard is not null)
                EnforceCIInExpression(row.Guard, ctx);
            foreach (var action in row.Actions)
                EnforceCIInAction(action, ctx);
        }

        foreach (var handler in ctx.EventHandlers)
        {
            foreach (var action in handler.Actions)
                EnforceCIInAction(action, ctx);
        }

        foreach (var rule in ctx.Rules)
        {
            EnforceCIInExpression(rule.Condition, ctx);
            if (rule.Guard is not null)
                EnforceCIInExpression(rule.Guard, ctx);
            EnforceCIInExpression(rule.Message, ctx);
        }

        foreach (var ensure in ctx.Ensures)
        {
            EnforceCIInExpression(ensure.Condition, ctx);
            if (ensure.Guard is not null)
                EnforceCIInExpression(ensure.Guard, ctx);
            EnforceCIInExpression(ensure.Message, ctx);
        }
    }

    /// <summary>Check a single action for CI violations.</summary>
    private static void EnforceCIInAction(TypedAction action, CheckContext ctx)
    {
        if (action is TypedInputAction ia)
        {
            EnforceCIInExpression(ia.InputExpression, ctx);
            if (ia.SecondaryExpression is not null)
                EnforceCIInExpression(ia.SecondaryExpression, ctx);
        }
    }

    /// <summary>
    /// Recursively walk a <see cref="TypedExpression"/> tree and emit CI enforcement
    /// diagnostics at each violation site.
    /// </summary>
    private static void EnforceCIInExpression(TypedExpression expr, CheckContext ctx)
    {
        switch (expr)
        {
            case TypedBinaryOp bin:
                if (Operations.GetMeta(bin.ResolvedOp) is BinaryOperationMeta
                        { HasCIVariant: true, CIDiagnosticCode: { } binaryDiagCode } &&
                    (IsCIExpression(bin.Left) || IsCIExpression(bin.Right)))
                {
                    var ciFieldName = GetCIFieldName(bin.Left, bin.Right);
                    ctx.Diagnostics.Add(Diagnostics.Create(binaryDiagCode, bin.Span, ciFieldName));
                }

                EnforceCIInExpression(bin.Left, ctx);
                EnforceCIInExpression(bin.Right, ctx);
                break;

            case TypedFunctionCall func:
                var funcMeta = Functions.GetMeta(func.ResolvedFunction);
                if (funcMeta is { HasCIVariant: true, CIDiagnosticCode: { } functionDiagCode } &&
                    func.Arguments.Length > 0 && IsCIExpression(func.Arguments[0]))
                {
                    var ciFieldName = ((TypedFieldRef)func.Arguments[0]).FieldName;
                    ctx.Diagnostics.Add(Diagnostics.Create(functionDiagCode, func.Span, ciFieldName));
                }

                foreach (var arg in func.Arguments)
                    EnforceCIInExpression(arg, ctx);
                break;

            case TypedUnaryOp un:
                EnforceCIInExpression(un.Operand, ctx);
                break;

            case TypedConditional cond:
                EnforceCIInExpression(cond.Condition, ctx);
                EnforceCIInExpression(cond.ThenBranch, ctx);
                EnforceCIInExpression(cond.ElseBranch, ctx);
                break;

            case TypedQuantifier quant:
                EnforceCIInExpression(quant.Collection, ctx);
                EnforceCIInExpression(quant.Predicate, ctx);
                break;

            case TypedMemberAccess mem:
                EnforceCIInExpression(mem.Object, ctx);
                break;

            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                {
                    if (seg is TypedHoleSegment hole)
                        EnforceCIInExpression(hole.Expression, ctx);
                }
                break;

            case TypedListLiteral list:
                foreach (var elem in list.Elements)
                    EnforceCIInExpression(elem, ctx);
                break;

            // Leaf nodes: TypedFieldRef, TypedArgRef, TypedLiteral, TypedTypedConstant,
            // TypedPostfixOp, TypedErrorExpression — no sub-expressions to walk
        }
    }

    /// <summary>Returns true if the expression resolves to a <c>~string</c> field reference.</summary>
    private static bool IsCIExpression(TypedExpression expr) =>
        expr is TypedFieldRef { IsCaseInsensitive: true };

    /// <summary>Returns the field name from whichever operand is a <c>~string</c> field reference.</summary>
    private static string GetCIFieldName(TypedExpression left, TypedExpression right) =>
        IsCIExpression(left) ? ((TypedFieldRef)left).FieldName : ((TypedFieldRef)right).FieldName;

}
