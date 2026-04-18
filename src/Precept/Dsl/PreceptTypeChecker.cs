using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Precept;

[Flags]
internal enum StaticValueKind
{
    None = 0,
    String = 1,
    Number = 2,
    Boolean = 4,
    Null = 8,
    Integer = 16,   // #29
    Decimal = 32,   // #27 (scaffold)
    OrderedChoice = 64,    // #25: choice field with 'ordered' modifier; behaves like String for equality/assignment
    UnorderedChoice = 128, // #25: choice field without 'ordered' modifier; behaves like String for equality/assignment
}

internal sealed record PreceptValidationDiagnostic(
    LanguageConstraint Constraint,
    string Message,
    int Line,
    int Column = 0,
    string? StateContext = null)
{
    public string DiagnosticCode => DiagnosticCatalog.ToDiagnosticCode(Constraint.Id);
}

internal sealed record PreceptTypeExpressionInfo(
    int Line,
    string ExpressionText,
    StaticValueKind Kind,
    string ScopeKind,
    string? StateContext = null,
    string? EventName = null);

internal sealed record PreceptTypeScopeInfo(
    int Line,
    string ScopeKind,
    IReadOnlyDictionary<string, StaticValueKind> Symbols,
    string? StateContext = null,
    string? EventName = null);

internal sealed class PreceptTypeContext(
    IReadOnlyList<PreceptTypeExpressionInfo> expressions,
    IReadOnlyList<PreceptTypeScopeInfo> scopes)
{
    public IReadOnlyList<PreceptTypeExpressionInfo> Expressions { get; } = expressions;

    public IReadOnlyList<PreceptTypeScopeInfo> Scopes { get; } = scopes;

    public PreceptTypeScopeInfo? FindBestScope(int oneBasedLine, string? stateContext = null, string? eventName = null)
    {
        var eligible = Scopes
            .Where(scope => scope.Line <= oneBasedLine)
            .Where(scope => stateContext is null || string.Equals(scope.StateContext, stateContext, StringComparison.Ordinal))
            .Where(scope => eventName is null || string.Equals(scope.EventName, eventName, StringComparison.Ordinal))
            .ToArray();

        if (eligible.Length == 0)
            return null;

        var bestLine = eligible.Max(scope => scope.Line);
        return eligible.LastOrDefault(scope => scope.Line == bestLine);
    }
}

internal sealed record TypeCheckResult(
    IReadOnlyList<PreceptValidationDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext,
    IReadOnlyList<string>? ComputedFieldOrder = null)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
}

internal sealed record ValidationResult(
    IReadOnlyList<PreceptValidationDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext,
    PreceptDefinition? ValidatedModel = null)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
}

internal static class PreceptTypeChecker
{
    public static TypeCheckResult Check(PreceptDefinition model)
    {
        var diagnostics = new List<PreceptValidationDiagnostic>();
        var expressions = new List<PreceptTypeExpressionInfo>();
        var scopes = new List<PreceptTypeScopeInfo>();

        GlobalProofContext dataFieldKinds = new GlobalProofContext(model.Fields.ToDictionary(
            field => field.Name,
            MapFieldContractKind,
            StringComparer.Ordinal));

        // Replace bespoke constraint-inspection loop with unified narrowing from rules.
        // Constraints desugar to synthetic rules at parse time (e.g., `positive` → `rule Field > 0`).
        // Unguarded rules are unconditional proofs; guarded rules are excluded because the
        // fact only holds when the guard is true.
        if (model.Rules is not null)
        {
            foreach (var rule in model.Rules.Where(r => r.WhenGuard is null))
            {
                dataFieldKinds = ApplyNarrowing(rule.Expression, dataFieldKinds, assumeTrue: true);
            }
        }

        // Slice 12: inject interval data from explicit min/max constraints.
        // nonnegative/positive flags are already covered by _flags injected above.
        {
            var markerDict = new Dictionary<string, StaticValueKind>(dataFieldKinds.Symbols, StringComparer.Ordinal);
            var fieldIntervals = CopyFieldIntervals(dataFieldKinds);
            foreach (var field in model.Fields)
            {
                if (field.Constraints is not { Count: > 0 }) continue;
                if (field.Type is not (PreceptScalarType.Number or PreceptScalarType.Integer or PreceptScalarType.Decimal)) continue;

                double? minVal = null;
                double? maxVal = null;
                foreach (var c in field.Constraints)
                {
                    if (c is FieldConstraint.Min m) minVal = m.Value;
                    else if (c is FieldConstraint.Max mx) maxVal = mx.Value;
                    else if (c is FieldConstraint.Positive) minVal = minVal is null || minVal.Value <= 0 ? double.Epsilon : minVal;
                    else if (c is FieldConstraint.Nonnegative) minVal ??= 0;
                }

                if (minVal is null && maxVal is null) continue;

                var lower = minVal ?? double.NegativeInfinity;
                var upper = maxVal ?? double.PositiveInfinity;
                var lowerInclusive = minVal.HasValue && !(field.Constraints.Any(c => c is FieldConstraint.Positive) && minVal.Value == double.Epsilon);
                if (field.Constraints.Any(c => c is FieldConstraint.Positive) && minVal.Value == double.Epsilon)
                {
                    lower = 0;
                    lowerInclusive = false;
                }
                var ival = new NumericInterval(lower, lowerInclusive, upper, maxVal.HasValue);
                fieldIntervals[field.Name] = ival;
            }
            dataFieldKinds = new GlobalProofContext(markerDict, new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts), fieldIntervals, CopyFlags(dataFieldKinds), CopyExprFacts(dataFieldKinds));
        }

        var eventArgKinds = model.Events.ToDictionary(
            evt => evt.Name,
            evt => evt.Args.ToDictionary(
                arg => arg.Name,
                MapFieldContractKind,
                StringComparer.Ordinal),
            StringComparer.Ordinal);

        var collectionFieldMap = model.CollectionFields.ToDictionary(
            field => field.Name,
            field => field,
            StringComparer.Ordinal);

        var stateEnsureNarrowings = BuildStateEnsureNarrowings(model, dataFieldKinds);
        var eventEnsureNarrowings = BuildEventEnsureNarrowings(model, eventArgKinds);
        ValidateTransitionRows(model, dataFieldKinds, eventArgKinds, collectionFieldMap, stateEnsureNarrowings, eventEnsureNarrowings, diagnostics, expressions, scopes);
        ValidateStateActions(model, dataFieldKinds, collectionFieldMap, stateEnsureNarrowings, diagnostics, expressions, scopes);
        ValidateFieldConstraints(model, diagnostics);
        ValidateRules(model, dataFieldKinds, eventArgKinds, diagnostics, expressions, scopes);

        var computedFieldOrder = ValidateComputedFields(model, dataFieldKinds, eventArgKinds, collectionFieldMap, diagnostics, expressions, scopes);

        return new TypeCheckResult(diagnostics, new PreceptTypeContext(expressions, scopes), computedFieldOrder);
    }

    internal static StaticValueKind MapFieldContractKind(PreceptField field)
    {
        if (field.Type == PreceptScalarType.Choice)
        {
            var choiceKind = field.IsOrdered ? StaticValueKind.OrderedChoice : StaticValueKind.UnorderedChoice;
            return field.IsNullable ? choiceKind | StaticValueKind.Null : choiceKind;
        }
        return MapKind(field.Type, field.IsNullable);
    }

    internal static StaticValueKind MapFieldContractKind(PreceptEventArg arg)
    {
        if (arg.Type == PreceptScalarType.Choice)
        {
            var choiceKind = arg.IsOrdered ? StaticValueKind.OrderedChoice : StaticValueKind.UnorderedChoice;
            return arg.IsNullable ? choiceKind | StaticValueKind.Null : choiceKind;
        }
        return MapKind(arg.Type, arg.IsNullable);
    }

    internal static StaticValueKind MapScalarType(PreceptScalarType type) => MapScalarTypeToKind(type);

    internal static bool TryGetLiteralKind(string label, out StaticValueKind kind)
    {
        switch (label)
        {
            case "true":
            case "false":
                kind = StaticValueKind.Boolean;
                return true;
            case "null":
                kind = StaticValueKind.Null;
                return true;
            default:
                kind = StaticValueKind.None;
                return false;
        }
    }

    internal static bool IsAssignableKind(StaticValueKind actual, StaticValueKind expected) => IsAssignable(actual, expected);

    internal static string FormatKinds(StaticValueKind kinds)
    {
        if (kinds == StaticValueKind.None)
            return "unknown";

        var labels = new List<string>(6);
        if (HasFlag(kinds, StaticValueKind.OrderedChoice)) labels.Add("ordered choice");
        else if (HasFlag(kinds, StaticValueKind.UnorderedChoice)) labels.Add("choice");
        else if (HasFlag(kinds, StaticValueKind.String)) labels.Add("string");
        if (HasFlag(kinds, StaticValueKind.Number)) labels.Add("number");
        if (HasFlag(kinds, StaticValueKind.Integer)) labels.Add("integer");
        if (HasFlag(kinds, StaticValueKind.Decimal)) labels.Add("decimal");
        if (HasFlag(kinds, StaticValueKind.Boolean)) labels.Add("boolean");
        if (HasFlag(kinds, StaticValueKind.Null)) labels.Add("null");
        return string.Join("|", labels);
    }

    private static void ValidateTransitionRows(
        PreceptDefinition model,
        GlobalProofContext dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        IReadOnlyDictionary<string, GlobalProofContext> stateEnsureNarrowings,
        IReadOnlyDictionary<string, GlobalProofContext> eventEnsureNarrowings,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        var fieldChoiceMap = model.Fields
            .Where(f => f.Type == PreceptScalarType.Choice && f.ChoiceValues?.Count > 0)
            .ToDictionary(f => f.Name, f => f.ChoiceValues!, StringComparer.Ordinal);
        var allStates = model.States.Select(static state => state.Name).ToArray();
        var rows = model.TransitionRows ?? Array.Empty<PreceptTransitionRow>();

        foreach (var group in rows.GroupBy(row => row.EventName, StringComparer.Ordinal))
        {
            var eventName = group.Key;
            var groupedRows = group
                .SelectMany(row => ExpandRowStates(row, allStates)
                    .Select(state => (State: state, Row: row)))
                .GroupBy(x => x.State, StringComparer.Ordinal);

            foreach (var stateGroup in groupedRows)
            {
                var baseSymbols = BuildSymbolKinds(
                    dataFieldKinds.Symbols,
                    eventArgKinds,
                    eventName,
                    model.CollectionFields,
                    stateEnsureNarrowings.TryGetValue(stateGroup.Key, out var stateNarrowing) ? stateNarrowing.Symbols : null);

                // Merge event ensure narrowings (dotted-form proof markers) into transition-row scope
                if (eventEnsureNarrowings.TryGetValue(eventName, out var eventNarrowing))
                {
                    foreach (var pair in eventNarrowing.Symbols)
                        baseSymbols[pair.Key] = pair.Value;
                }

                scopes.Add(new PreceptTypeScopeInfo(
                    stateGroup.Min(x => x.Row.SourceLine),
                    "transition-base",
                    new Dictionary<string, StaticValueKind>(baseSymbols, StringComparer.Ordinal),
                    stateGroup.Key,
                    eventName));

                GlobalProofContext branchContext = dataFieldKinds.ChildMerging(baseSymbols, stateNarrowing, eventNarrowing);

                // C47: detect identical guard text for the same (state, event) group
                var seenGuards = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var item in stateGroup.OrderBy(x => x.Row.SourceLine))
                {
                    var row = item.Row;
                    GlobalProofContext setContext = branchContext;

                    // C47: duplicate guard detection for this (state, event) group
                    if (row.WhenGuard is not null && !string.IsNullOrWhiteSpace(row.WhenText))
                    {
                        var normalizedGuard = Regex.Replace(
                            row.WhenText!.Trim(), @"\s+", " ");
                        if (seenGuards.TryGetValue(normalizedGuard, out var firstLine))
                        {
                            diagnostics.Add(new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C47,
                                DiagnosticCatalog.C47.FormatMessage(
                                    ("guard", normalizedGuard),
                                    ("state", item.State),
                                    ("event", eventName),
                                    ("sourceLine", firstLine)),
                                row.SourceLine,
                                StateContext: item.State));
                        }
                        else
                        {
                            seenGuards[normalizedGuard] = row.SourceLine;
                        }
                    }

                    if (row.WhenGuard is not null && !string.IsNullOrWhiteSpace(row.WhenText))
                    {
                        scopes.Add(new PreceptTypeScopeInfo(
                            row.SourceLine,
                            "when",
                            new Dictionary<string, StaticValueKind>(branchContext.Symbols, StringComparer.Ordinal),
                            item.State,
                            eventName));
                        ValidateExpression(
                            row.WhenGuard,
                            row.WhenText!,
                            row.SourceLine,
                            branchContext,
                            StaticValueKind.Boolean,
                            "when predicate",
                            diagnostics,
                            expressions,
                            stateContext: item.State,
                            isBooleanRulePosition: true);

                        // SYNC:CONSTRAINT:C97/C98: dead/tautological guard detection
                        AssessGuard(row.WhenGuard, row.WhenText!, row.SourceLine,
                            dataFieldKinds.FieldIntervals, diagnostics, item.State);

                        setContext = ApplyNarrowing(row.WhenGuard, branchContext, assumeTrue: true);
                        branchContext = ApplyNarrowing(row.WhenGuard, branchContext, assumeTrue: false);
                    }

                    scopes.Add(new PreceptTypeScopeInfo(
                        row.SourceLine,
                        "transition-actions",
                        new Dictionary<string, StaticValueKind>(setContext.Symbols, StringComparer.Ordinal),
                        item.State,
                        eventName));

                    foreach (var assignment in row.SetAssignments)
                    {
                        if (!dataFieldKinds.Symbols.TryGetValue(assignment.Key, out var targetKind))
                            continue;

                        ValidateExpression(
                            assignment.Expression,
                            assignment.ExpressionText,
                            assignment.SourceLine > 0 ? assignment.SourceLine : row.SourceLine,
                            setContext,
                            targetKind,
                            $"set target '{assignment.Key}'",
                            diagnostics,
                            expressions,
                            stateContext: item.State);

                        // SYNC:CONSTRAINT:C68: literal must be a member of the choice set
                        if (fieldChoiceMap.TryGetValue(assignment.Key, out var choiceVals)
                            && assignment.Expression is PreceptLiteralExpression { Value: string memberLiteral }
                            && !choiceVals.Contains(memberLiteral, StringComparer.Ordinal))
                        {
                            diagnostics.Add(new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C68,
                                DiagnosticCatalog.C68.FormatMessage(
                                    ("value", memberLiteral),
                                    ("values", string.Join(", ", choiceVals.Select(v => $"\"{v}\""))),
                                    ("name", assignment.Key)),
                                assignment.SourceLine > 0 ? assignment.SourceLine : row.SourceLine,
                                Column: assignment.Expression.Position?.StartColumn ?? 0,
                                StateContext: item.State));
                        }

                        // SYNC:CONSTRAINT:C94: assignment provably outside field constraint range
                        if (dataFieldKinds.FieldIntervals.TryGetValue(assignment.Key, out var constraintIval94))
                        {
                            var rhsIval = setContext.IntervalOf(assignment.Expression);
                            if (!rhsIval.IsUnknown && NumericInterval.AreDisjoint(rhsIval, constraintIval94))
                            {
                                var assessment = new ProofAssessment(
                                    ProofRequirement.AssignmentConstraint, ProofOutcome.Contradiction,
                                    assignment.Key, rhsIval, ProofAttribution.None,
                                    ConstraintInterval: constraintIval94);
                                diagnostics.Add(new PreceptValidationDiagnostic(
                                    DiagnosticCatalog.C94,
                                    ProofDiagnosticRenderer.Render(assessment),
                                    assignment.SourceLine > 0 ? assignment.SourceLine : row.SourceLine,
                                    Column: assignment.Expression.Position?.StartColumn ?? 0,
                                    StateContext: item.State));
                            }
                        }

                        // Layer 1: thread post-assignment proof state into subsequent assignments.
                        setContext = ApplyAssignmentNarrowing(assignment.Key, assignment.Expression, setContext);
                    }

                    ValidateCollectionMutations(
                        row.CollectionMutations,
                        setContext,
                        dataFieldKinds.Symbols,
                        collectionFieldMap,
                        diagnostics,
                        expressions,
                        item.State,
                        row.SourceLine,
                        eventName);
                }
            }
        }
    }

    private static void ValidateStateActions(
        PreceptDefinition model,
        GlobalProofContext dataFieldKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        IReadOnlyDictionary<string, GlobalProofContext> stateEnsureNarrowings,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        if (model.StateActions is null || model.StateActions.Count == 0)
            return;

        var fieldChoiceMap = model.Fields
            .Where(f => f.Type == PreceptScalarType.Choice && f.ChoiceValues?.Count > 0)
            .ToDictionary(f => f.Name, f => f.ChoiceValues!, StringComparer.Ordinal);

        foreach (var action in model.StateActions)
        {
            // Build data-only symbols with collection accessors, narrowed by state ensures
            var baseSymbols = new Dictionary<string, StaticValueKind>(
                stateEnsureNarrowings.TryGetValue(action.State, out var narrowed) ? narrowed.Symbols : dataFieldKinds.Symbols,
                StringComparer.Ordinal);

            foreach (var col in model.CollectionFields)
            {
                var innerKind = MapScalarTypeToKind(col.InnerType);
                baseSymbols[$"{col.Name}.count"] = StaticValueKind.Number;

                if (col.CollectionKind == PreceptCollectionKind.Set)
                {
                    baseSymbols[$"{col.Name}.min"] = innerKind;
                    baseSymbols[$"{col.Name}.max"] = innerKind;
                }

                if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                    baseSymbols[$"{col.Name}.peek"] = innerKind;
            }

            foreach (var field in model.Fields)
            {
                if (field.Type == PreceptScalarType.String)
                    baseSymbols[$"{field.Name}.length"] = StaticValueKind.Number;
            }

            scopes.Add(new PreceptTypeScopeInfo(
                action.SourceLine,
                "state-action",
                new Dictionary<string, StaticValueKind>(baseSymbols, StringComparer.Ordinal),
                action.State));

            // Layer 1: thread post-assignment proof state into subsequent assignments.
            GlobalProofContext assignmentContext = new GlobalProofContext(baseSymbols, new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts), CopyFieldIntervals(dataFieldKinds), CopyFlags(dataFieldKinds), CopyExprFacts(dataFieldKinds));

            foreach (var assignment in action.SetAssignments)
            {
                if (!dataFieldKinds.Symbols.TryGetValue(assignment.Key, out var targetKind))
                    continue;

                ValidateExpression(
                    assignment.Expression,
                    assignment.ExpressionText,
                    assignment.SourceLine > 0 ? assignment.SourceLine : action.SourceLine,
                    assignmentContext,
                    targetKind,
                    $"set target '{assignment.Key}'",
                    diagnostics,
                    expressions,
                    stateContext: action.State);

                // SYNC:CONSTRAINT:C68: literal must be a member of the choice set
                if (fieldChoiceMap.TryGetValue(assignment.Key, out var choiceVals)
                    && assignment.Expression is PreceptLiteralExpression { Value: string memberLiteral }
                    && !choiceVals.Contains(memberLiteral, StringComparer.Ordinal))
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C68,
                        DiagnosticCatalog.C68.FormatMessage(
                            ("value", memberLiteral),
                            ("values", string.Join(", ", choiceVals.Select(v => $"\"{v}\""))),
                            ("name", assignment.Key)),
                        assignment.SourceLine > 0 ? assignment.SourceLine : action.SourceLine,
                        Column: assignment.Expression.Position?.StartColumn ?? 0,
                        StateContext: action.State));
                }

                // SYNC:CONSTRAINT:C94: assignment provably outside field constraint range
                if (dataFieldKinds.FieldIntervals.TryGetValue(assignment.Key, out var constraintIval94))
                {
                    var rhsIval = assignmentContext.IntervalOf(assignment.Expression);
                    if (!rhsIval.IsUnknown && NumericInterval.AreDisjoint(rhsIval, constraintIval94))
                    {
                        var assessment = new ProofAssessment(
                            ProofRequirement.AssignmentConstraint, ProofOutcome.Contradiction,
                            assignment.Key, rhsIval, ProofAttribution.None,
                            ConstraintInterval: constraintIval94);
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C94,
                            ProofDiagnosticRenderer.Render(assessment),
                            assignment.SourceLine > 0 ? assignment.SourceLine : action.SourceLine,
                            Column: assignment.Expression.Position?.StartColumn ?? 0,
                            StateContext: action.State));
                    }
                }

                assignmentContext = ApplyAssignmentNarrowing(assignment.Key, assignment.Expression, assignmentContext);
            }

            ValidateCollectionMutations(
                action.CollectionMutations,
                assignmentContext,
                dataFieldKinds.Symbols,
                collectionFieldMap,
                diagnostics,
                expressions,
                action.State,
                action.SourceLine,
                eventName: null);
        }
    }

    /// <summary>
    /// Validates field/arg-level constraint suffixes for type compatibility (C57),
    /// contradiction/duplicate (C58), and default-value violations (C59).
    /// Runs before <see cref="ValidateRules"/> so errors are attributed to constraints,
    /// not to the synthetic rules they generate.
    /// </summary>
    private static void ValidateFieldConstraints(
        PreceptDefinition model,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        foreach (var field in model.Fields)
        {
            // SYNC:CONSTRAINT:C62
            // SYNC:CONSTRAINT:C63
            // SYNC:CONSTRAINT:C66
            ValidateChoiceField(field.Name, field.Type, field.ChoiceValues, field.IsOrdered, field.SourceLine, diagnostics);

            // SYNC:CONSTRAINT:C64
            if (field.Type == PreceptScalarType.Choice &&
                field.HasDefaultValue &&
                field.DefaultValue is string defaultStr &&
                field.ChoiceValues is { Count: > 0 } choiceVals &&
                !choiceVals.Contains(defaultStr, StringComparer.Ordinal))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C64,
                    DiagnosticCatalog.C64.FormatMessage(
                        ("value", defaultStr),
                        ("values", string.Join(", ", choiceVals.Select(v => $"\"{v}\""))),
                        ("name", field.Name)),
                    field.SourceLine));
            }

            if (field.Constraints is not { Count: > 0 }) goto ValidateConstraints;
            // SYNC:CONSTRAINT:C57
            ValidateConstraintTypes(field.Name, field.Type, isCollection: false, field.Constraints, field.SourceLine, diagnostics);
            // SYNC:CONSTRAINT:C58
            ValidateConstraintDuplicates(field.Name, field.Constraints, field.SourceLine, diagnostics);
            // SYNC:CONSTRAINT:C59
            if (field.HasDefaultValue)
                ValidateConstraintDefault(field.Name, field.DefaultValue, field.Constraints, field.SourceLine, diagnostics);

            ValidateConstraints: ;
        }

        foreach (var col in model.CollectionFields)
        {
            // SYNC:CONSTRAINT:C62
            // SYNC:CONSTRAINT:C63
            ValidateChoiceField(col.Name, col.InnerType, col.ChoiceValues, isOrdered: false, col.SourceLine, diagnostics);

            if (col.Constraints is not { Count: > 0 }) continue;
            // SYNC:CONSTRAINT:C57
            ValidateConstraintTypes(col.Name, null, isCollection: true, col.Constraints, col.SourceLine, diagnostics);
            // SYNC:CONSTRAINT:C58
            ValidateConstraintDuplicates(col.Name, col.Constraints, col.SourceLine, diagnostics);
        }

        foreach (var evt in model.Events)
        {
            foreach (var arg in evt.Args)
            {
                // SYNC:CONSTRAINT:C62
                // SYNC:CONSTRAINT:C63
                // SYNC:CONSTRAINT:C66
                ValidateChoiceField(arg.Name, arg.Type, arg.ChoiceValues, arg.IsOrdered, arg.SourceLine, diagnostics);

                if (arg.Constraints is not { Count: > 0 }) continue;
                // SYNC:CONSTRAINT:C57
                ValidateConstraintTypes(arg.Name, arg.Type, isCollection: false, arg.Constraints, arg.SourceLine, diagnostics);
                // SYNC:CONSTRAINT:C58
                ValidateConstraintDuplicates(arg.Name, arg.Constraints, arg.SourceLine, diagnostics);
                // SYNC:CONSTRAINT:C59
                if (arg.HasDefaultValue)
                    ValidateConstraintDefault(arg.Name, arg.DefaultValue, arg.Constraints, arg.SourceLine, diagnostics);
            }
        }
    }

    /// <summary>Validates choice type metadata: C62 (empty set), C63 (duplicates), C66 (ordered on non-choice).</summary>
    private static void ValidateChoiceField(
        string name,
        PreceptScalarType type,
        IReadOnlyList<string>? choiceValues,
        bool isOrdered,
        int sourceLine,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        // SYNC:CONSTRAINT:C66
        if (isOrdered && type != PreceptScalarType.Choice)
        {
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C66,
                DiagnosticCatalog.C66.FormatMessage(
                    ("name", name),
                    ("type", type.ToString().ToLowerInvariant())),
                sourceLine));
        }

        if (type != PreceptScalarType.Choice)
            return;

        // SYNC:CONSTRAINT:C62
        if (choiceValues == null || choiceValues.Count == 0)
        {
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C62,
                DiagnosticCatalog.C62.FormatMessage(("name", name)),
                sourceLine));
            return;
        }

        // SYNC:CONSTRAINT:C63
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var val in choiceValues)
        {
            if (!seen.Add(val))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C63,
                    DiagnosticCatalog.C63.FormatMessage(("value", val), ("name", name)),
                    sourceLine));
                break; // one diagnostic per field for duplicate
            }
        }
    }

    private static void ValidateConstraintTypes(
        string name,
        PreceptScalarType? scalarType,
        bool isCollection,
        IReadOnlyList<FieldConstraint> constraints,
        int sourceLine,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        foreach (var c in constraints)
        {
            bool valid = (c, isCollection, scalarType) switch
            {
                // Number-only constraints
                (FieldConstraint.Nonnegative, false, PreceptScalarType.Number) => true,
                (FieldConstraint.Positive,    false, PreceptScalarType.Number) => true,
                (FieldConstraint.Min,         false, PreceptScalarType.Number) => true,
                (FieldConstraint.Max,         false, PreceptScalarType.Number) => true,
                // Integer also accepts numeric range constraints
                (FieldConstraint.Nonnegative, false, PreceptScalarType.Integer) => true,
                (FieldConstraint.Positive,    false, PreceptScalarType.Integer) => true,
                (FieldConstraint.Min,         false, PreceptScalarType.Integer) => true,
                (FieldConstraint.Max,         false, PreceptScalarType.Integer) => true,
                // Decimal accepts numeric range constraints and maxplaces
                (FieldConstraint.Nonnegative, false, PreceptScalarType.Decimal) => true,
                (FieldConstraint.Positive,    false, PreceptScalarType.Decimal) => true,
                (FieldConstraint.Min,         false, PreceptScalarType.Decimal) => true,
                (FieldConstraint.Max,         false, PreceptScalarType.Decimal) => true,
                // Maxplaces only valid on Decimal
                (FieldConstraint.Maxplaces,   false, PreceptScalarType.Decimal) => true,
                // String-only constraints
                (FieldConstraint.Notempty,    false, PreceptScalarType.String) => true,
                (FieldConstraint.Minlength,   false, PreceptScalarType.String) => true,
                (FieldConstraint.Maxlength,   false, PreceptScalarType.String) => true,
                // Collection constraints
                (FieldConstraint.Notempty,    true,  _) => true,
                (FieldConstraint.Mincount,    true,  _) => true,
                (FieldConstraint.Maxcount,    true,  _) => true,
                _ => false
            };

            if (!valid)
            {
                var typeLabel = isCollection ? "collection" : scalarType?.ToString().ToLowerInvariant() ?? "unknown";
                var constraintLabel = ConstraintLabel(c);
                // SYNC:CONSTRAINT:C61
                if (c is FieldConstraint.Maxplaces)
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C61,
                        DiagnosticCatalog.C61.MessageTemplate,
                        sourceLine));
                }
                // SYNC:CONSTRAINT:C57
                else
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C57,
                        DiagnosticCatalog.C57.FormatMessage(
                            ("constraint", constraintLabel),
                            ("type", typeLabel)),
                        sourceLine));
                }
            }
        }
    }

    private static void ValidateConstraintDuplicates(
        string name,
        IReadOnlyList<FieldConstraint> constraints,
        int sourceLine,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        // Detect same-kind duplicates (e.g., two min constraints regardless of value)
        var kindSeen = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var c in constraints)
        {
            var kind = ConstraintKindKey(c);
            if (kindSeen.ContainsKey(kind))
            {
                // SYNC:CONSTRAINT:C58
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C58,
                    DiagnosticCatalog.C58.FormatMessage(
                        ("message", $"Duplicate constraint '{kind}' on field '{name}'.")),
                    sourceLine));
            }
            else
            {
                kindSeen[kind] = true;
            }
        }

        // Detect subsumption: positive is strictly stronger than nonnegative;
        // having both means nonnegative can never fire independently — it is dead code.
        bool hasNonneg = constraints.Any(static c => c is FieldConstraint.Nonnegative);
        bool hasPositive = constraints.Any(static c => c is FieldConstraint.Positive);
        if (hasNonneg && hasPositive)
        {
            // SYNC:CONSTRAINT:C58
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C58,
                DiagnosticCatalog.C58.FormatMessage(
                    ("message", "Constraint 'nonnegative' is subsumed by 'positive'.")) ,
                sourceLine));
        }

        // Detect contradictory range constraints
        double? minVal = null, maxVal = null;
        foreach (var c in constraints)
        {
            if (c is FieldConstraint.Min mn) minVal = mn.Value;
            if (c is FieldConstraint.Max mx) maxVal = mx.Value;
            if (c is FieldConstraint.Mincount mc) minVal = mc.Value;
            if (c is FieldConstraint.Maxcount mc2) maxVal = mc2.Value;
        }
        if (minVal.HasValue && maxVal.HasValue && minVal.Value > maxVal.Value)
        {
            var c1 = constraints.FirstOrDefault(c => c is FieldConstraint.Min or FieldConstraint.Mincount);
            var c2 = constraints.FirstOrDefault(c => c is FieldConstraint.Max or FieldConstraint.Maxcount);
            // SYNC:CONSTRAINT:C58
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C58,
                DiagnosticCatalog.C58.FormatMessage(
                    ("message", $"Contradictory constraints: '{ConstraintLabel(c1!)}' and '{ConstraintLabel(c2!)}' define an empty valid range.")),
                sourceLine));
        }
    }

    private static void ValidateConstraintDefault(
        string name,
        object? defaultValue,
        IReadOnlyList<FieldConstraint> constraints,
        int sourceLine,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        foreach (var c in constraints)
        {
            bool violated = (c, defaultValue) switch
            {
                (FieldConstraint.Nonnegative, double d) => d < 0,
                (FieldConstraint.Positive,    double d) => d <= 0,
                (FieldConstraint.Min mn,      double d) => d < mn.Value,
                (FieldConstraint.Max mx,      double d) => d > mx.Value,
                (FieldConstraint.Nonnegative, long l) => l < 0,
                (FieldConstraint.Positive,    long l) => l <= 0,
                (FieldConstraint.Min mn,      long l) => l < mn.Value,
                (FieldConstraint.Max mx,      long l) => l > mx.Value,
                (FieldConstraint.Notempty,    string s) => s.Length == 0,
                (FieldConstraint.Minlength ml, string s) => s.Length < ml.Value,
                (FieldConstraint.Maxlength ml, string s) => s.Length > ml.Value,
                _ => false
            };

            if (violated)
            {
                var valueLabel = defaultValue switch
                {
                    string s => $"\"{s}\"",
                    long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _ => defaultValue?.ToString() ?? "null"
                };
                // SYNC:CONSTRAINT:C59
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C59,
                    DiagnosticCatalog.C59.FormatMessage(
                        ("value", valueLabel),
                        ("constraint", ConstraintLabel(c))),
                    sourceLine));
            }
        }
    }

    private static string ConstraintKindKey(FieldConstraint c) => c switch
    {
        FieldConstraint.Nonnegative => "nonnegative",
        FieldConstraint.Positive    => "positive",
        FieldConstraint.Notempty    => "notempty",
        FieldConstraint.Min         => "min",
        FieldConstraint.Max         => "max",
        FieldConstraint.Minlength   => "minlength",
        FieldConstraint.Maxlength   => "maxlength",
        FieldConstraint.Mincount    => "mincount",
        FieldConstraint.Maxcount    => "maxcount",
        FieldConstraint.Maxplaces   => "maxplaces",
        _                           => c.GetType().Name.ToLowerInvariant()
    };

    private static string ConstraintLabel(FieldConstraint c) => c switch
    {
        FieldConstraint.Nonnegative => "nonnegative",
        FieldConstraint.Positive    => "positive",
        FieldConstraint.Notempty    => "notempty",
        FieldConstraint.Min mn      => $"min {mn.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        FieldConstraint.Max mx      => $"max {mx.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        FieldConstraint.Minlength ml => $"minlength {ml.Value}",
        FieldConstraint.Maxlength ml => $"maxlength {ml.Value}",
        FieldConstraint.Mincount mc  => $"mincount {mc.Value}",
        FieldConstraint.Maxcount mc  => $"maxcount {mc.Value}",
        FieldConstraint.Maxplaces mp => $"maxplaces {mp.Places}",
        _                           => c.GetType().Name.ToLowerInvariant()
    };

    // ═══════════════════════════════════════════════════════════════
    // Computed field validation (C83–C88)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates computed/derived field expressions: type checking, scope restrictions,
    /// dependency graph analysis, cycle detection, and edit/set protection.
    /// Returns the topological evaluation order of computed fields (null if none).
    /// </summary>
    private static IReadOnlyList<string>? ValidateComputedFields(
        PreceptDefinition model,
        GlobalProofContext dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        var computedFields = model.Fields.Where(static f => f.IsComputed).ToArray();
        if (computedFields.Length == 0)
            return null;

        var computedFieldNames = new HashSet<string>(
            computedFields.Select(static f => f.Name), StringComparer.Ordinal);

        // Nullable field names (for C83 checking)
        var nullableFieldNames = new HashSet<string>(
            model.Fields.Where(static f => f.IsNullable).Select(static f => f.Name),
            StringComparer.Ordinal);

        // Event arg dotted forms: "EventName.ArgName" (for C84 checking)
        var eventArgDottedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var evt in model.Events)
            foreach (var arg in evt.Args)
                eventArgDottedNames.Add($"{evt.Name}.{arg.Name}");

        // Unsafe collection accessors (for C85 checking)
        var unsafeAccessors = new HashSet<string>(StringComparer.Ordinal) { "peek", "min", "max" };

        // Build full data-symbols scope for expression type checking (same as ValidateRules)
        var dataSymbols = new Dictionary<string, StaticValueKind>(dataFieldKinds.Symbols, StringComparer.Ordinal);
        foreach (var col in model.CollectionFields)
        {
            dataSymbols[$"{col.Name}.count"] = StaticValueKind.Number;
            var innerKind = MapScalarTypeToKind(col.InnerType);
            if (col.CollectionKind == PreceptCollectionKind.Set)
            {
                dataSymbols[$"{col.Name}.min"] = innerKind;
                dataSymbols[$"{col.Name}.max"] = innerKind;
            }
            if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                dataSymbols[$"{col.Name}.peek"] = innerKind;
        }
        foreach (var field in model.Fields)
        {
            if (field.Type == PreceptScalarType.String)
                dataSymbols[$"{field.Name}.length"] = StaticValueKind.Number;
        }

        // Dependency graph: computed field name → set of computed field names it depends on
        var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var field in computedFields)
        {
            var expectedKind = MapScalarTypeToKind(field.Type);

            // Type-check the derived expression against the full data scope
            ValidateExpression(
                field.DerivedExpression!,
                field.DerivedExpressionText!,
                field.SourceLine,
                new GlobalProofContext(dataSymbols),
                expectedKind,
                $"computed field '{field.Name}'",
                diagnostics,
                expressions);

            // Walk expression for computed-field-specific restrictions
            var deps = new HashSet<string>(StringComparer.Ordinal);
            WalkComputedFieldExpression(
                field.DerivedExpression!,
                field.SourceLine,
                nullableFieldNames,
                eventArgDottedNames,
                unsafeAccessors,
                collectionFieldMap,
                computedFieldNames,
                deps,
                diagnostics);
            dependencies[field.Name] = deps;
        }

        // Topological sort with cycle detection
        var sorted = new List<string>();
        // 0 = not visited, 1 = in progress, 2 = done
        var visitState = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in computedFieldNames)
            visitState[name] = 0;

        bool hasCycle = false;
        foreach (var name in computedFieldNames)
        {
            if (visitState[name] == 0)
            {
                var path = new List<string>();
                if (!TopologicalVisit(name, dependencies, visitState, sorted, path, computedFields, diagnostics))
                    hasCycle = true;
            }
        }

        // SYNC:CONSTRAINT:C87 — computed field in edit declaration
        if (model.EditBlocks is { Count: > 0 })
        {
            foreach (var editBlock in model.EditBlocks)
            {
                foreach (var fieldName in editBlock.FieldNames)
                {
                    if (computedFieldNames.Contains(fieldName))
                    {
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C87,
                            DiagnosticCatalog.C87.FormatMessage(("fieldName", fieldName)),
                            editBlock.SourceLine));
                    }
                }
            }
        }

        // SYNC:CONSTRAINT:C88 — computed field as set target
        var computedFieldLookup = computedFields.ToDictionary(
            static f => f.Name, static f => f, StringComparer.Ordinal);
        if (model.TransitionRows is { Count: > 0 })
        {
            foreach (var row in model.TransitionRows)
            {
                foreach (var assignment in row.SetAssignments)
                {
                    if (computedFieldLookup.TryGetValue(assignment.Key, out var cf))
                    {
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C88,
                            DiagnosticCatalog.C88.FormatMessage(
                                ("fieldName", assignment.Key),
                                ("expression", cf.DerivedExpressionText ?? "")),
                            assignment.SourceLine > 0 ? assignment.SourceLine : row.SourceLine));
                    }
                }
            }
        }

        if (model.StateActions is { Count: > 0 })
        {
            foreach (var action in model.StateActions)
            {
                foreach (var assignment in action.SetAssignments)
                {
                    if (computedFieldLookup.TryGetValue(assignment.Key, out var cf))
                    {
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C88,
                            DiagnosticCatalog.C88.FormatMessage(
                                ("fieldName", assignment.Key),
                                ("expression", cf.DerivedExpressionText ?? "")),
                            assignment.SourceLine > 0 ? assignment.SourceLine : action.SourceLine));
                    }
                }
            }
        }

        return hasCycle ? null : sorted;
    }

    /// <summary>
    /// Walks a computed field expression tree and emits C83/C84/C85 for prohibited references.
    /// Also collects dependencies on other computed fields for cycle detection.
    /// </summary>
    private static void WalkComputedFieldExpression(
        PreceptExpression expr,
        int sourceLine,
        HashSet<string> nullableFieldNames,
        HashSet<string> eventArgDottedNames,
        HashSet<string> unsafeAccessors,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        HashSet<string> computedFieldNames,
        HashSet<string> dependencies,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        switch (expr)
        {
            case PreceptIdentifierExpression id:
                // Check for event argument references (C84)
                if (id.Member is not null)
                {
                    var dottedName = $"{id.Name}.{id.Member}";

                    // Event arg form: EventName.ArgName
                    if (eventArgDottedNames.Contains(dottedName))
                    {
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C84,
                            DiagnosticCatalog.C84.FormatMessage(("name", dottedName)),
                            sourceLine));
                        break;
                    }

                    // Collection accessor form: CollectionName.accessor
                    if (collectionFieldMap.ContainsKey(id.Name))
                    {
                        if (unsafeAccessors.Contains(id.Member))
                        {
                            // SYNC:CONSTRAINT:C85
                            diagnostics.Add(new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C85,
                                DiagnosticCatalog.C85.FormatMessage(("accessor", id.Member)),
                                sourceLine));
                        }
                        // .count is safe — no diagnostic needed
                        break;
                    }
                }

                // Plain identifier — check nullable field reference (C83)
                if (id.Member is null && nullableFieldNames.Contains(id.Name))
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C83,
                        DiagnosticCatalog.C83.FormatMessage(("fieldName", id.Name)),
                        sourceLine));
                }

                // Track dependencies on other computed fields
                if (id.Member is null && computedFieldNames.Contains(id.Name))
                    dependencies.Add(id.Name);

                break;

            case PreceptBinaryExpression bin:
                WalkComputedFieldExpression(bin.Left, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                WalkComputedFieldExpression(bin.Right, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                break;

            case PreceptUnaryExpression unary:
                WalkComputedFieldExpression(unary.Operand, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                break;

            case PreceptParenthesizedExpression paren:
                WalkComputedFieldExpression(paren.Inner, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                break;

            case PreceptFunctionCallExpression fn:
                foreach (var arg in fn.Arguments)
                    WalkComputedFieldExpression(arg, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                break;

            case PreceptConditionalExpression cond:
                WalkComputedFieldExpression(cond.Condition, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                WalkComputedFieldExpression(cond.ThenBranch, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                WalkComputedFieldExpression(cond.ElseBranch, sourceLine, nullableFieldNames, eventArgDottedNames, unsafeAccessors, collectionFieldMap, computedFieldNames, dependencies, diagnostics);
                break;

            case PreceptLiteralExpression:
                break;
        }
    }

    /// <summary>
    /// DFS visit for topological sort. Returns false if a cycle is detected.
    /// </summary>
    private static bool TopologicalVisit(
        string name,
        Dictionary<string, HashSet<string>> dependencies,
        Dictionary<string, int> visitState,
        List<string> sorted,
        List<string> path,
        PreceptField[] computedFields,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        if (visitState.TryGetValue(name, out var state))
        {
            if (state == 2) return true;  // already done
            if (state == 1)
            {
                // Cycle detected — build the cycle path
                var cycleStart = path.IndexOf(name);
                var cyclePath = path.Skip(cycleStart).Append(name).ToArray();
                var cycleStr = string.Join(" \u2192 ", cyclePath);
                var field = computedFields.FirstOrDefault(f =>
                    string.Equals(f.Name, name, StringComparison.Ordinal));
                // SYNC:CONSTRAINT:C86
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C86,
                    DiagnosticCatalog.C86.FormatMessage(("cycle", cycleStr)),
                    field?.SourceLine ?? 0));
                return false;
            }
        }

        visitState[name] = 1; // in progress
        path.Add(name);

        bool ok = true;
        if (dependencies.TryGetValue(name, out var deps))
        {
            foreach (var dep in deps)
            {
                if (!TopologicalVisit(dep, dependencies, visitState, sorted, path, computedFields, diagnostics))
                    ok = false;
            }
        }

        path.RemoveAt(path.Count - 1);
        visitState[name] = 2; // done
        sorted.Add(name);
        return ok;
    }

    private static void ValidateRules(
        PreceptDefinition model,
        GlobalProofContext dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        var dataSymbols = new Dictionary<string, StaticValueKind>(dataFieldKinds.Symbols, StringComparer.Ordinal);
        foreach (var col in model.CollectionFields)
        {
            dataSymbols[$"{col.Name}.count"] = StaticValueKind.Number;

            var innerKind = MapScalarTypeToKind(col.InnerType);
            if (col.CollectionKind == PreceptCollectionKind.Set)
            {
                dataSymbols[$"{col.Name}.min"] = innerKind;
                dataSymbols[$"{col.Name}.max"] = innerKind;
            }

            if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                dataSymbols[$"{col.Name}.peek"] = innerKind;
        }

        foreach (var field in model.Fields)
        {
            if (field.Type == PreceptScalarType.String)
                dataSymbols[$"{field.Name}.length"] = StaticValueKind.Number;
        }

        scopes.Add(new PreceptTypeScopeInfo(1, "data-rules", new Dictionary<string, StaticValueKind>(dataSymbols, StringComparer.Ordinal)));

        if (model.Rules is not null)
        {
            foreach (var rule in model.Rules)
            {
                ValidateExpression(
                    rule.Expression,
                    rule.ExpressionText,
                    rule.SourceLine,
                    new GlobalProofContext(
                        dataSymbols,
                        new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                        CopyFieldIntervals(dataFieldKinds),
                        CopyFlags(dataFieldKinds),
                        CopyExprFacts(dataFieldKinds)),
                    StaticValueKind.Boolean,
                    "rule",
                    diagnostics,
                    expressions,
                    isBooleanRulePosition: true);

                if (rule.WhenGuard is not null)
                {
                    ValidateExpression(
                        rule.WhenGuard,
                        rule.WhenText!,
                        rule.SourceLine,
                        new GlobalProofContext(
                            dataSymbols,
                            new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                            CopyFieldIntervals(dataFieldKinds),
                            CopyFlags(dataFieldKinds),
                            CopyExprFacts(dataFieldKinds)),
                        StaticValueKind.Boolean,
                        "rule when guard",
                        diagnostics,
                        expressions,
                        isBooleanRulePosition: true);

                    // SYNC:CONSTRAINT:C69
                    CheckCrossScopeGuardIdentifiers(rule.WhenGuard, dataSymbols, rule.SourceLine, diagnostics);
                }
            }
        }

        // SYNC:CONSTRAINT:C95: non-synthetic rule contradicts field constraints
        if (model.Rules is not null)
        {
            // Build constraint-only intervals from field definitions (min/max/positive/nonnegative).
            var fieldConstraintIntervals = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
            foreach (var field in model.Fields)
            {
                if (field.Constraints is not { Count: > 0 }) continue;
                if (field.Type is not (PreceptScalarType.Number or PreceptScalarType.Integer or PreceptScalarType.Decimal)) continue;

                double? minVal = null;
                double? maxVal = null;
                foreach (var c in field.Constraints)
                {
                    if (c is FieldConstraint.Min m) minVal = m.Value;
                    else if (c is FieldConstraint.Max mx) maxVal = mx.Value;
                    else if (c is FieldConstraint.Positive) minVal = minVal is null || minVal.Value <= 0 ? double.Epsilon : minVal;
                    else if (c is FieldConstraint.Nonnegative) minVal ??= 0;
                }

                if (minVal is null && maxVal is null) continue;

                var lower = minVal ?? double.NegativeInfinity;
                var upper = maxVal ?? double.PositiveInfinity;
                // positive uses open lower bound (0, +∞); min uses closed [min, …]
                var lowerInclusive = minVal.HasValue && !(field.Constraints.Any(c => c is FieldConstraint.Positive) && minVal.Value == double.Epsilon);
                if (field.Constraints.Any(c => c is FieldConstraint.Positive) && minVal.Value == double.Epsilon)
                {
                    lower = 0;
                    lowerInclusive = false;
                }
                var ival = new NumericInterval(lower, lowerInclusive, upper, maxVal.HasValue);
                fieldConstraintIntervals[field.Name] = ival;
            }

            foreach (var rule in model.Rules.Where(r => !r.IsSynthetic && r.WhenGuard is null))
            {
                if (TryExtractSingleFieldComparison(rule.Expression, out var fieldName, out var ruleInterval)
                    && fieldConstraintIntervals.TryGetValue(fieldName, out var constraintIval))
                {
                    // SYNC:CONSTRAINT:C95: contradictory rule detection
                    if (NumericInterval.AreDisjoint(ruleInterval, constraintIval))
                    {
                        var assessment = new ProofAssessment(
                            ProofRequirement.RuleSatisfiability, ProofOutcome.Contradiction,
                            fieldName, ruleInterval, ProofAttribution.None,
                            ConstraintInterval: constraintIval,
                            ConstraintDescription: rule.ExpressionText);
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C95,
                            ProofDiagnosticRenderer.Render(assessment),
                            rule.SourceLine));
                    }
                    // SYNC:CONSTRAINT:C96: vacuous rule detection
                    // If the constraint interval is entirely within the rule interval,
                    // the rule adds no new information — it's always true given constraints.
                    else if (!constraintIval.IsUnknown && NumericInterval.Contains(ruleInterval, constraintIval))
                    {
                        var assessment = new ProofAssessment(
                            ProofRequirement.RuleVacuity, ProofOutcome.Satisfied,
                            fieldName, ruleInterval, ProofAttribution.None,
                            ConstraintInterval: constraintIval,
                            ConstraintDescription: rule.ExpressionText);
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C96,
                            ProofDiagnosticRenderer.Render(assessment),
                            rule.SourceLine));
                    }
                }
            }
        }

        if (model.StateEnsures is not null)
        {
            foreach (var stateEnsure in model.StateEnsures)
            {
                ValidateExpression(
                    stateEnsure.Expression,
                    stateEnsure.ExpressionText,
                    stateEnsure.SourceLine,
                    new GlobalProofContext(
                        dataSymbols,
                        new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                        CopyFieldIntervals(dataFieldKinds),
                        CopyFlags(dataFieldKinds),
                        CopyExprFacts(dataFieldKinds)),
                    StaticValueKind.Boolean,
                    $"state ensure on '{stateEnsure.State}'",
                    diagnostics,
                    expressions,
                    stateContext: stateEnsure.State,
                    isBooleanRulePosition: true);

                if (stateEnsure.WhenGuard is not null)
                {
                    ValidateExpression(
                        stateEnsure.WhenGuard,
                        stateEnsure.WhenText!,
                        stateEnsure.SourceLine,
                        new GlobalProofContext(
                            dataSymbols,
                            new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                            CopyFieldIntervals(dataFieldKinds),
                            CopyFlags(dataFieldKinds),
                            CopyExprFacts(dataFieldKinds)),
                        StaticValueKind.Boolean,
                        "state ensure when guard",
                        diagnostics,
                        expressions,
                        stateContext: stateEnsure.State,
                        isBooleanRulePosition: true);

                    // SYNC:CONSTRAINT:C69
                    CheckCrossScopeGuardIdentifiers(stateEnsure.WhenGuard, dataSymbols, stateEnsure.SourceLine, diagnostics);
                }
            }
        }

        if (model.EventEnsures is not null)
        {
            foreach (var eventEnsure in model.EventEnsures)
            {
                var symbols = BuildEventEnsureSymbols(model, eventArgKinds, eventEnsure.EventName);
                scopes.Add(new PreceptTypeScopeInfo(
                    eventEnsure.SourceLine,
                    "event-ensure",
                    new Dictionary<string, StaticValueKind>(symbols, StringComparer.Ordinal),
                    null,
                    eventEnsure.EventName));
                ValidateExpression(
                    eventEnsure.Expression,
                    eventEnsure.ExpressionText,
                    eventEnsure.SourceLine,
                    new GlobalProofContext(
                        symbols,
                        new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                        CopyFieldIntervals(dataFieldKinds),
                        CopyFlags(dataFieldKinds),
                        CopyExprFacts(dataFieldKinds)),
                    StaticValueKind.Boolean,
                    $"event ensure on '{eventEnsure.EventName}'",
                    diagnostics,
                    expressions,
                    eventName: eventEnsure.EventName,
                    isBooleanRulePosition: true);

                if (eventEnsure.WhenGuard is not null)
                {
                    ValidateExpression(
                        eventEnsure.WhenGuard,
                        eventEnsure.WhenText!,
                        eventEnsure.SourceLine,
                        new GlobalProofContext(
                            symbols,
                            new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                            CopyFieldIntervals(dataFieldKinds),
                            CopyFlags(dataFieldKinds),
                            CopyExprFacts(dataFieldKinds)),
                        StaticValueKind.Boolean,
                        "event ensure when guard",
                        diagnostics,
                        expressions,
                        eventName: eventEnsure.EventName,
                        isBooleanRulePosition: true);

                    // SYNC:CONSTRAINT:C69
                    CheckCrossScopeGuardIdentifiers(eventEnsure.WhenGuard, symbols, eventEnsure.SourceLine, diagnostics);
                }
            }
        }

        if (model.EditBlocks is not null)
        {
            foreach (var editBlock in model.EditBlocks)
            {
                if (editBlock.WhenGuard is not null)
                {
                    ValidateExpression(
                        editBlock.WhenGuard,
                        editBlock.WhenText!,
                        editBlock.SourceLine,
                        new GlobalProofContext(
                            dataSymbols,
                            new Dictionary<LinearForm, RelationalFact>(dataFieldKinds.RelationalFacts),
                            CopyFieldIntervals(dataFieldKinds),
                            CopyFlags(dataFieldKinds),
                            CopyExprFacts(dataFieldKinds)),
                        StaticValueKind.Boolean,
                        "edit when guard",
                        diagnostics,
                        expressions,
                        isBooleanRulePosition: true);

                    // SYNC:CONSTRAINT:C69
                    CheckCrossScopeGuardIdentifiers(editBlock.WhenGuard, dataSymbols, editBlock.SourceLine, diagnostics);
                }
            }
        }
    }

    /// <summary>
    /// Walks a guard expression and emits C69 for any identifier not in the allowed symbol scope.
    /// </summary>
    private static void CheckCrossScopeGuardIdentifiers(
        PreceptExpression expr,
        IReadOnlyDictionary<string, StaticValueKind> allowedSymbols,
        int sourceLine,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        switch (expr)
        {
            case PreceptIdentifierExpression id:
                var fullName = id.SubMember is not null
                    ? $"{id.Name}.{id.Member}.{id.SubMember}"
                    : id.Member is not null ? $"{id.Name}.{id.Member}" : id.Name;
                if (!allowedSymbols.ContainsKey(fullName) && !allowedSymbols.ContainsKey(id.Name))
                {
                    // SYNC:CONSTRAINT:C69
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C69,
                        DiagnosticCatalog.C69.FormatMessage(("name", fullName)),
                        sourceLine,
                        Column: id.Position?.StartColumn ?? 0));
                }
                break;

            case PreceptBinaryExpression bin:
                CheckCrossScopeGuardIdentifiers(bin.Left, allowedSymbols, sourceLine, diagnostics);
                CheckCrossScopeGuardIdentifiers(bin.Right, allowedSymbols, sourceLine, diagnostics);
                break;

            case PreceptUnaryExpression unary:
                CheckCrossScopeGuardIdentifiers(unary.Operand, allowedSymbols, sourceLine, diagnostics);
                break;

            case PreceptParenthesizedExpression paren:
                CheckCrossScopeGuardIdentifiers(paren.Inner, allowedSymbols, sourceLine, diagnostics);
                break;

            case PreceptFunctionCallExpression fn:
                foreach (var arg in fn.Arguments)
                    CheckCrossScopeGuardIdentifiers(arg, allowedSymbols, sourceLine, diagnostics);
                break;

            case PreceptLiteralExpression:
                break;
        }
    }

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

    private static IEnumerable<string> ExpandRowStates(PreceptTransitionRow row, IReadOnlyList<string> allStates)
    {
        if (string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase))
            return allStates;

        return [row.FromState];
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

    private static Dictionary<string, StaticValueKind> BuildSymbolKinds(
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName,
        IReadOnlyList<PreceptCollectionField> collectionFields,
        IReadOnlyDictionary<string, StaticValueKind>? stateSymbols)
    {
        var symbols = stateSymbols is not null
            ? new Dictionary<string, StaticValueKind>(stateSymbols, StringComparer.Ordinal)
            : new Dictionary<string, StaticValueKind>(dataFieldKinds, StringComparer.Ordinal);

        if (eventArgKinds.TryGetValue(eventName, out var eventArgs))
        {
            foreach (var pair in eventArgs)
            {
                // Only dotted form (EventName.ArgName) is valid in transition-row scope.
                // Bare arg names are valid only in event-ensure scope (see BuildEventEnsureSymbols).
                symbols[$"{eventName}.{pair.Key}"] = pair.Value;
                if (HasFlag(pair.Value, StaticValueKind.String))
                    symbols[$"{eventName}.{pair.Key}.length"] = StaticValueKind.Number;
            }
        }

        foreach (var col in collectionFields)
        {
            var innerKind = MapScalarTypeToKind(col.InnerType);
            symbols[$"{col.Name}.count"] = StaticValueKind.Number;

            if (col.CollectionKind == PreceptCollectionKind.Set)
            {
                symbols[$"{col.Name}.min"] = innerKind;
                symbols[$"{col.Name}.max"] = innerKind;
            }

            if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                symbols[$"{col.Name}.peek"] = innerKind;
        }

        foreach (var pair in dataFieldKinds)
        {
            if (HasFlag(pair.Value, StaticValueKind.String))
                symbols[$"{pair.Key}.length"] = StaticValueKind.Number;
        }

        return symbols;
    }

    private static void ValidateExpression(
        PreceptExpression expression,
        string expressionText,
        int sourceLine,
        GlobalProofContext context,
        StaticValueKind expectedKind,
        string expectedLabel,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        string? stateContext = null,
        string? eventName = null,
        bool isBooleanRulePosition = false)
    {
        if (!TryInferKind(expression, context, out var actualKind, out var diagnostic))
        {
            diagnostics.Add(diagnostic! with
            {
                Line = sourceLine > 0 ? sourceLine : diagnostic.Line,
                Column = diagnostic.Column != 0 ? diagnostic.Column : expression.Position?.StartColumn ?? 0,
                StateContext = stateContext
            });
            return;
        }

        // Collect any non-blocking warnings from inference (e.g., C93 unproven divisor).
        if (diagnostic is not null)
        {
            diagnostics.Add(diagnostic with
            {
                Line = sourceLine > 0 ? sourceLine : diagnostic.Line,
                Column = diagnostic.Column != 0 ? diagnostic.Column : expression.Position?.StartColumn ?? 0,
                StateContext = stateContext
            });
        }

        if (IsAssignable(actualKind, expectedKind))
        {
            expressions.Add(new PreceptTypeExpressionInfo(
                sourceLine,
                expressionText,
                actualKind,
                expectedLabel,
                stateContext,
                eventName));
            return;
        }

        LanguageConstraint constraint;
        string message;
        if (isBooleanRulePosition)
        {
            constraint = DiagnosticCatalog.C46;
            message = $"{expectedLabel} must be a boolean expression, but expression produces {FormatKinds(actualKind)}.";
        }
        else
        {
            constraint = HasFlag(actualKind, StaticValueKind.Null) && !HasFlag(expectedKind, StaticValueKind.Null)
                ? DiagnosticCatalog.C42
                // SYNC:CONSTRAINT:C60
                : IsNarrowingToInteger(actualKind, expectedKind)
                    ? DiagnosticCatalog.C60
                    : DiagnosticCatalog.C39;
            message = constraint == DiagnosticCatalog.C60
                ? BuildC60Message(actualKind, expectedLabel)
                : TryBuildNumericMismatchMessage(actualKind, expectedKind, expectedLabel)
                  ?? $"{expectedLabel} type mismatch: expected {FormatKinds(expectedKind)} but expression produces {FormatKinds(actualKind)}.";
        }

        expressions.Add(new PreceptTypeExpressionInfo(
            sourceLine,
            expressionText,
            actualKind,
            expectedLabel,
            stateContext,
            eventName));

        diagnostics.Add(new PreceptValidationDiagnostic(
            constraint,
            message,
            sourceLine,
            Column: expression.Position?.StartColumn ?? 0,
            StateContext: stateContext));
    }

    private static bool TryInferKind(
        PreceptExpression expression,
        GlobalProofContext context,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;
        var symbols = context.Symbols;

        switch (expression)
        {
            case PreceptLiteralExpression literal:
                kind = MapLiteralKind(literal.Value);
                return true;

            case PreceptIdentifierExpression identifier:
            {
                var key = identifier.SubMember is not null
                    ? $"{identifier.Name}.{identifier.Member}.{identifier.SubMember}"
                    : identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
                if (!symbols.TryGetValue(key, out kind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C38,
                        $"unknown identifier '{key}'.",
                        0,
                        Column: identifier.Position?.StartColumn ?? 0);
                    return false;
                }

                // C56: .length on a nullable string requires an explicit null guard before access.
                // SYNC:CONSTRAINT:C56
                if (identifier.Member == "length" &&
                    symbols.TryGetValue(identifier.Name, out var baseKind) &&
                    HasFlag(baseKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C56,
                        DiagnosticCatalog.C56.FormatMessage(("field", identifier.Name)),
                        0,
                        Column: identifier.Position?.StartColumn ?? 0);
                    return false;
                }

                // C56 three-level form: EventName.ArgName.length — nullable arg requires null guard.
                // SYNC:CONSTRAINT:C56
                if (identifier.SubMember == "length")
                {
                    var argKey = $"{identifier.Name}.{identifier.Member}";
                    if (symbols.TryGetValue(argKey, out var argKind) && HasFlag(argKind, StaticValueKind.Null))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C56,
                            DiagnosticCatalog.C56.FormatMessage(("field", argKey)),
                            0,
                            Column: identifier.Position?.StartColumn ?? 0);
                        return false;
                    }
                }

                return true;
            }

            case PreceptParenthesizedExpression parenthesized:
                return TryInferKind(parenthesized.Inner, context, out kind, out diagnostic);

            case PreceptUnaryExpression unary:
            {
                if (!TryInferKind(unary.Operand, context, out var operandKind, out diagnostic))
                    return false;

                if (unary.Operator == "not")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Boolean))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C40,
                            "operator 'not' requires boolean operand.",
                            0,
                            Column: unary.Operand.Position?.StartColumn ?? 0);
                        return false;
                    }

                    kind = StaticValueKind.Boolean;
                    return true;
                }

                if (unary.Operator == "-")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Number) &&
                        !IsExactly(operandKind, StaticValueKind.Integer))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C40,
                            "unary '-' requires numeric operand.",
                            0,
                            Column: unary.Operand.Position?.StartColumn ?? 0);
                        return false;
                    }

                    kind = operandKind; // preserve Integer or Number
                    return true;
                }

                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C40,
                    $"unsupported unary operator '{unary.Operator}'.",
                    0,
                    Column: unary.Position?.StartColumn ?? 0);
                return false;
            }

            case PreceptBinaryExpression binary:
                return TryInferBinaryKind(binary, context, out kind, out diagnostic);

            case PreceptFunctionCallExpression fn:
                return TryInferFunctionCallKind(fn, context, out kind, out diagnostic);

            // SYNC:CONSTRAINT:C78
            // SYNC:CONSTRAINT:C79
            case PreceptConditionalExpression cond:
            {
                // Infer condition type
                if (!TryInferKind(cond.Condition, context, out var condKind, out diagnostic))
                    return false;

                // C78: condition must be non-nullable boolean
                if (!IsExactly(condKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C78,
                        DiagnosticCatalog.C78.FormatMessage(("actual", FormatKinds(condKind))),
                        0,
                        Column: cond.Condition.Position?.StartColumn ?? 0);
                    return false;
                }

                // Null-narrow symbols for then-branch (condition assumed true)
                var thenContext = ApplyNarrowing(cond.Condition, context, assumeTrue: true);
                if (!TryInferKind(cond.ThenBranch, thenContext, out var thenKind, out diagnostic))
                    return false;

                // Else branch uses original symbols (no reverse narrowing)
                if (!TryInferKind(cond.ElseBranch, context, out var elseKind, out diagnostic))
                    return false;

                // C79: branches must produce compatible scalar types (with integer widening)
                var thenBase = thenKind & ~StaticValueKind.Null;
                var elseBase = elseKind & ~StaticValueKind.Null;
                StaticValueKind resultBase;
                if (thenBase == elseBase)
                {
                    resultBase = thenBase;
                }
                else if (IsAssignable(thenBase, elseBase))
                {
                    // then-branch widens to else-branch's type (e.g. integer → number)
                    resultBase = elseBase;
                }
                else if (IsAssignable(elseBase, thenBase))
                {
                    // else-branch widens to then-branch's type (e.g. integer → number)
                    resultBase = thenBase;
                }
                else
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C79,
                        BuildC79Message(thenKind, elseKind),
                        0,
                        Column: cond.ElseBranch.Position?.StartColumn ?? 0);
                    return false;
                }

                // Result type is the wider base + Null if either branch is nullable
                kind = resultBase | ((thenKind | elseKind) & StaticValueKind.Null);
                return true;
            }

            default:
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C39,
                    "unsupported expression node.",
                    0,
                    Column: expression.Position?.StartColumn ?? 0);
                return false;
        }
    }

    private static bool TryInferFunctionCallKind(
        PreceptFunctionCallExpression fn,
        GlobalProofContext context,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;
        var symbols = context.Symbols;

        // SYNC:CONSTRAINT:C71 — unknown function name
        if (!FunctionRegistry.TryGetFunction(fn.Name, out var funcDef))
        {
            diagnostic = new PreceptValidationDiagnostic(
                DiagnosticCatalog.C71,
                $"Unknown function '{fn.Name}'.",
                0,
                Column: fn.Position?.StartColumn ?? 0);
            return false;
        }

        // Infer all argument kinds before overload resolution.
        var argKinds = new StaticValueKind[fn.Arguments.Length];
        for (int i = 0; i < fn.Arguments.Length; i++)
        {
            if (!TryInferKind(fn.Arguments[i], context, out argKinds[i], out diagnostic))
                return false;

            // SYNC:CONSTRAINT:C77 — nullable arguments
            if ((argKinds[i] & StaticValueKind.Null) != 0)
            {
                var argName = fn.Arguments[i] is PreceptIdentifierExpression id ? id.Name : $"argument {i + 1}";
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C77,
                    $"Function '{fn.Name}' does not accept nullable arguments. '{argName}' may be null. Add a null check.",
                    0,
                    Column: fn.Arguments[i].Position?.StartColumn ?? 0);
                return false;
            }
        }

        // Find the best matching overload (arity + argument types).
        FunctionOverload? matched = null;
        foreach (var overload in funcDef.Overloads)
        {
            bool arityOk = overload.MinArity.HasValue
                ? fn.Arguments.Length >= overload.MinArity.Value
                : fn.Arguments.Length == overload.Parameters.Length;

            if (!arityOk)
                continue;

            bool typesOk = true;
            if (overload.MinArity.HasValue)
            {
                // Variadic: single parameter type applies to all arguments.
                var paramType = overload.Parameters[0].AcceptedTypes;
                for (int i = 0; i < fn.Arguments.Length; i++)
                {
                    if ((argKinds[i] & paramType) == 0)
                    {
                        typesOk = false;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < overload.Parameters.Length; i++)
                {
                    if ((argKinds[i] & overload.Parameters[i].AcceptedTypes) == 0)
                    {
                        typesOk = false;
                        break;
                    }
                }
            }

            if (typesOk)
            {
                matched = overload;
                break;
            }
        }

        if (matched is null)
        {
            // SYNC:CONSTRAINT:C75 — pow exponent must be integer
            if (fn.Name == "pow" && fn.Arguments.Length == 2 &&
                IsNumericKind(argKinds[1]) && !IsExactly(argKinds[1], StaticValueKind.Integer))
            {
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C75,
                    $"pow() exponent must be integer type, but got {KindLabel(argKinds[1])}.",
                    0,
                    Column: fn.Arguments[1].Position?.StartColumn ?? 0);
                return false;
            }

            // Try to pinpoint a specific parameter type mismatch (C73).
            var arityMatch = funcDef.Overloads.LastOrDefault(o =>
                o.MinArity.HasValue
                    ? fn.Arguments.Length >= o.MinArity.Value
                    : fn.Arguments.Length == o.Parameters.Length);

            if (arityMatch is not null)
            {
                // SYNC:CONSTRAINT:C73 — argument type mismatch
                if (arityMatch.MinArity.HasValue)
                {
                    var param = arityMatch.Parameters[0];
                    for (int i = 0; i < fn.Arguments.Length; i++)
                    {
                        if ((argKinds[i] & param.AcceptedTypes) == 0)
                        {
                            diagnostic = new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C73,
                                $"{fn.Name}() no matching overload: {param.Name} argument expects {KindLabel(param.AcceptedTypes)} but got {KindLabel(argKinds[i])}.",
                                0,
                                Column: fn.Arguments[i].Position?.StartColumn ?? 0);
                            return false;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < arityMatch.Parameters.Length; i++)
                    {
                        var param = arityMatch.Parameters[i];
                        if ((argKinds[i] & param.AcceptedTypes) == 0)
                        {
                            diagnostic = new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C73,
                                $"{fn.Name}() no matching overload: {param.Name} argument expects {KindLabel(param.AcceptedTypes)} but got {KindLabel(argKinds[i])}.",
                                0,
                                Column: fn.Arguments[i].Position?.StartColumn ?? 0);
                            return false;
                        }
                    }
                }
            }

            // SYNC:CONSTRAINT:C72 — no matching overload
            diagnostic = new PreceptValidationDiagnostic(
                DiagnosticCatalog.C72,
                $"{fn.Name}() called with {fn.Arguments.Length} argument(s), but no matching overload found.",
                0,
                Column: fn.Position?.StartColumn ?? 0);
            return false;
        }

        // Validate special parameter constraints on the matched overload.
        var paramLoop = matched.MinArity.HasValue
            ? Enumerable.Range(0, fn.Arguments.Length).Select(i => (ParamIndex: 0, ArgIndex: i))
            : Enumerable.Range(0, matched.Parameters.Length).Select(i => (ParamIndex: i, ArgIndex: i));

        foreach (var (paramIndex, argIndex) in paramLoop)
        {
            var param = matched.Parameters[paramIndex];

            // SYNC:CONSTRAINT:C74 — round precision must be non-negative integer literal
            if (param.Constraint == FunctionArgConstraint.MustBeIntegerLiteral)
            {
                if (fn.Arguments[argIndex] is not PreceptLiteralExpression { Value: long lv } || lv < 0)
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C74,
                        "round() precision argument must be a non-negative integer literal.",
                        0,
                        Column: fn.Arguments[argIndex].Position?.StartColumn ?? 0);
                    return false;
                }
            }

            // SYNC:CONSTRAINT:C76 — sqrt requires non-negative proof
            if (param.Constraint == FunctionArgConstraint.RequiresNonNegativeProof)
            {
                var arg = fn.Arguments[argIndex];
                var assessment = AssessNonnegativeArgument(arg, context);
                if (assessment is not null && assessment.Outcome != ProofOutcome.Satisfied)
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        assessment.DiagnosticCode,
                        ProofDiagnosticRenderer.Render(assessment),
                        0,
                        Column: arg.Position?.StartColumn ?? 0);
                    return false;
                }
            }
        }

        kind = matched.ReturnType;
        return true;
    }

    private static bool TryInferBinaryKind(
        PreceptBinaryExpression binary,
        GlobalProofContext context,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;
        var symbols = context.Symbols;

        switch (binary.Operator)
        {
            case "and":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'and' requires boolean operands.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                var rightContext = ApplyNarrowing(binary.Left, context, assumeTrue: true);
                if (!TryInferKind(binary.Right, rightContext, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'and' requires boolean operands.", 0, Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "or":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'or' requires boolean operands.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                var rightContext = ApplyNarrowing(binary.Left, context, assumeTrue: false);
                if (!TryInferKind(binary.Right, rightContext, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'or' requires boolean operands.", 0, Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "+":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic) ||
                    !TryInferKind(binary.Right, context, out var rightKind, out diagnostic))
                    return false;

                var stringCandidate = IsExactly(leftKind, StaticValueKind.String) && IsExactly(rightKind, StaticValueKind.String);

                if (stringCandidate)
                {
                    kind = StaticValueKind.String;
                    return true;
                }

                if (IsNumericKind(leftKind) && IsNumericKind(rightKind))
                {
                    kind = ResolveNumericResultKind(leftKind, rightKind);
                    return true;
                }

                diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator '+' requires number+number or string+string.", 0, Column: binary.Position?.StartColumn ?? 0);
                return false;
            }

            case "-":
            case "*":
            case "/":
            case "%":
            case ">":
            case ">=":
            case "<":
            case "<=":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic) ||
                    !TryInferKind(binary.Right, context, out var rightKind, out diagnostic))
                    return false;

                // Choice-type ordinal checks apply only to the comparison operators, not arithmetic.
                if (binary.Operator is ">" or ">=" or "<" or "<=")
                {
                    var leftBase = leftKind & ~StaticValueKind.Null;
                    var rightBase = rightKind & ~StaticValueKind.Null;

                    // SYNC:CONSTRAINT:C65: ordinal operator on a choice field that lacks 'ordered'
                    if (leftBase == StaticValueKind.UnorderedChoice)
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C65,
                            DiagnosticCatalog.C65.FormatMessage(("operator", binary.Operator)),
                            0,
                            Column: binary.Left.Position?.StartColumn ?? 0);
                        return false;
                    }

                    // SYNC:CONSTRAINT:C67: ordinal comparison between two choice fields — rank is field-local
                    if (leftBase == StaticValueKind.OrderedChoice && rightBase == StaticValueKind.OrderedChoice)
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C67,
                            DiagnosticCatalog.C67.FormatMessage(("operator", binary.Operator)),
                            0,
                            Column: binary.Position?.StartColumn ?? 0);
                        return false;
                    }

                    // Valid ordinal comparison: ordered choice field vs string literal (or plain string)
                    if (leftBase == StaticValueKind.OrderedChoice
                        && (rightBase == StaticValueKind.String || rightBase == StaticValueKind.UnorderedChoice))
                    {
                        kind = StaticValueKind.Boolean;
                        return true;
                    }
                }

                if (!IsNumericKind(leftKind) || !IsNumericKind(rightKind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' requires numeric operands.",
                        0,
                        Column: binary.Position?.StartColumn ?? 0);
                    return false;
                }

                // SYNC:CONSTRAINT:C92 — provably-zero divisor
                // SYNC:CONSTRAINT:C93 — unproven divisor safety
                // Principle #8 ("never guess"): C92 is error (provably zero — contradiction).
                // C93 is error (unproven divisor — risks IEEE 754 Infinity/NaN at runtime).
                // Unified assessment: C92 = proven contradiction, C93 = unresolved obligation.
                if (binary.Operator is "/" or "%")
                {
                    var assessment = AssessDivisorSafety(binary.Right, context);
                    if (assessment is not null && assessment.Outcome != ProofOutcome.Satisfied)
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            assessment.DiagnosticCode,
                            ProofDiagnosticRenderer.Render(assessment),
                            0,
                            Column: binary.Right.Position?.StartColumn ?? 0);
                        if (assessment.Outcome == ProofOutcome.Contradiction)
                            return false;
                    }
                }

                kind = binary.Operator is ">" or ">=" or "<" or "<="
                    ? StaticValueKind.Boolean
                    : ResolveNumericResultKind(leftKind, rightKind);
                return true;
            }

            case "==":
            case "!=":
            {
                if (!TryInferKind(binary.Left, context, out var leftEqKind, out diagnostic) ||
                    !TryInferKind(binary.Right, context, out var rightEqKind, out diagnostic))
                    return false;

                // Normalize choice kinds to String for equality compatibility:
                // 'Status == "Active"' is valid whether Status is ordered or unordered choice.
                var leftEqFamily = NormalizeChoiceKind(leftEqKind & ~StaticValueKind.Null);
                var rightEqFamily = NormalizeChoiceKind(rightEqKind & ~StaticValueKind.Null);
                var leftIsNull = leftEqKind == StaticValueKind.Null;
                var rightIsNull = rightEqKind == StaticValueKind.Null;

                // null literal compared to a non-nullable operand is always wrong
                if (leftIsNull && !HasFlag(rightEqKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' cannot compare non-nullable {FormatKinds(rightEqKind)} with null.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                if (rightIsNull && !HasFlag(leftEqKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' cannot compare non-nullable {FormatKinds(leftEqKind)} with null.", 0, Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                // Cross-type equality: both sides have non-null scalar families that differ
                // Allow Integer <=> Number mix (widening)
                var isIntNumberMix =
                    (leftEqFamily == StaticValueKind.Integer && rightEqFamily == StaticValueKind.Number) ||
                    (leftEqFamily == StaticValueKind.Number  && rightEqFamily == StaticValueKind.Integer);
                // Allow Decimal <=> Integer mix (widening) and Decimal <=> Decimal
                var isDecimalMix =
                    (leftEqFamily == StaticValueKind.Decimal && rightEqFamily == StaticValueKind.Integer) ||
                    (leftEqFamily == StaticValueKind.Integer && rightEqFamily == StaticValueKind.Decimal) ||
                    (leftEqFamily == StaticValueKind.Decimal && rightEqFamily == StaticValueKind.Decimal);
                if (!isIntNumberMix && !isDecimalMix && leftEqFamily != StaticValueKind.None && rightEqFamily != StaticValueKind.None && leftEqFamily != rightEqFamily)
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' requires operands of the same type, but found {FormatKinds(leftEqKind)} and {FormatKinds(rightEqKind)}.", 0, Column: binary.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "contains":
            {
                if (binary.Left is not PreceptIdentifierExpression { Member: null } collectionIdentifier)
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "'contains' requires a collection field on the left side.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                if (!TryInferKind(binary.Right, context, out var rightKind, out diagnostic))
                    return false;

                var collectionKey = $"{collectionIdentifier.Name}.count";
                if (!symbols.ContainsKey(collectionKey))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C38, $"unknown identifier '{collectionIdentifier.Name}'.", 0, Column: collectionIdentifier.Position?.StartColumn ?? 0);
                    return false;
                }

                var innerKeyCandidates = new[]
                {
                    $"{collectionIdentifier.Name}.min",
                    $"{collectionIdentifier.Name}.peek",
                    $"{collectionIdentifier.Name}.max"
                };

                var innerKind = innerKeyCandidates
                    .Where(symbols.ContainsKey)
                    .Select(key => symbols[key])
                    .DefaultIfEmpty(StaticValueKind.None)
                    .First();

                if (innerKind != StaticValueKind.None && !IsAssignable(rightKind, innerKind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C41,
                        $"operator 'contains' requires RHS of type {FormatKinds(innerKind)} but expression produces {FormatKinds(rightKind)}.",
                        0,
                        Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            default:
                diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, $"unsupported binary operator '{binary.Operator}'.", 0, Column: binary.Position?.StartColumn ?? 0);
                return false;
        }
    }

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
                var left  = context.IntervalOf(binary.Left);
                var right = context.IntervalOf(binary.Right);
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
                var thenInterval = thenContext.IntervalOf(cond.ThenBranch);
                var elseInterval = elseContext.IntervalOf(cond.ElseBranch);
                return NumericInterval.Hull(thenInterval, elseInterval);
            }

            default:
                return NumericInterval.Unknown;
        }
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
            var rhsInterval = context.IntervalOf(rhs);
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
    /// Returns a mutable copy of <paramref name="context"/>'s typed relational fact store.
    /// Used by <see cref="TryApplyNumericComparisonNarrowing"/> to produce copy-on-write updates.
    /// </summary>
    private static Dictionary<LinearForm, RelationalFact> CopyRelationalFacts(GlobalProofContext context)
    {
        var copy = new Dictionary<LinearForm, RelationalFact>();
        foreach (var kvp in context.RelationalFacts)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed field interval store.
    /// Used for copy-on-write propagation when constructing derived <see cref="GlobalProofContext"/> instances.
    /// </summary>
    private static Dictionary<string, NumericInterval> CopyFieldIntervals(GlobalProofContext context)
    {
        var copy = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
        foreach (var kvp in context.FieldIntervals)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed numeric flags store.
    /// </summary>
    private static Dictionary<string, NumericFlags> CopyFlags(GlobalProofContext context)
    {
        var copy = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
        foreach (var kvp in context.Flags)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed expression-level interval store.
    /// </summary>
    private static Dictionary<LinearForm, NumericInterval> CopyExprFacts(GlobalProofContext context)
    {
        var copy = new Dictionary<LinearForm, NumericInterval>();
        foreach (var kvp in context.ExprFacts)
            copy[kvp.Key] = kvp.Value;
        return copy;
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
        var interval = context.IntervalOf(divisor);
        var subject = DescribeExpression(divisor);

        // Provably zero: contradiction → C92
        if (interval is { Lower: 0, Upper: 0, LowerInclusive: true, UpperInclusive: true })
            return new ProofAssessment(
                ProofRequirement.NonzeroDivisor, ProofOutcome.Contradiction,
                subject, interval, ProofAttribution.None);

        // Provably nonzero via interval: satisfied → no diagnostic
        if (interval.ExcludesZero)
            return new ProofAssessment(
                ProofRequirement.NonzeroDivisor, ProofOutcome.Satisfied,
                subject, interval, ProofAttribution.None);

        // Flag-based nonzero proof for identifiers: the Nonzero flag (from rule != 0,
        // when != 0, etc.) cannot be expressed as a single contiguous interval, so
        // check the flags dictionary directly for identifier expressions.
        if (TryGetIdentifierKey(divisor, out var key) &&
            context.Flags.TryGetValue(key, out var flags) &&
            (flags & (NumericFlags.Nonzero | NumericFlags.Positive)) != 0)
            return new ProofAssessment(
                ProofRequirement.NonzeroDivisor, ProofOutcome.Satisfied,
                subject, interval, ProofAttribution.None);

        // Obligation: zero still possible or unknown → C93
        return new ProofAssessment(
            ProofRequirement.NonzeroDivisor, ProofOutcome.Obligation,
            subject, interval, ProofAttribution.None);
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
                StateContext: stateContext));
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
                StateContext: stateContext));
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

        var interval = context.IntervalOf(arg);

        // Provably negative: contradiction
        if (interval is { Upper: < 0 })
            return new ProofAssessment(
                ProofRequirement.NonnegativeArgument, ProofOutcome.Contradiction,
                subject, interval, ProofAttribution.None);

        // Obligation: may be negative
        return new ProofAssessment(
            ProofRequirement.NonnegativeArgument, ProofOutcome.Obligation,
            subject, interval, ProofAttribution.None);
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

    private static void ValidateCollectionMutations(
        IReadOnlyList<PreceptCollectionMutation>? mutations,
        GlobalProofContext context,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        string stateContext,
        int fallbackLine,
        string? eventName)
    {
        if (mutations is null || mutations.Count == 0)
            return;

        foreach (var mutation in mutations)
        {
            if (!collectionFieldMap.TryGetValue(mutation.TargetField, out var collectionField))
                continue;

            var innerKind = MapScalarTypeToKind(collectionField.InnerType);
            var line = mutation.SourceLine > 0 ? mutation.SourceLine : fallbackLine;

            switch (mutation.Verb)
            {
                case PreceptCollectionMutationVerb.Add:
                case PreceptCollectionMutationVerb.Remove:
                case PreceptCollectionMutationVerb.Enqueue:
                case PreceptCollectionMutationVerb.Push:
                    if (mutation.Expression is null || string.IsNullOrWhiteSpace(mutation.ExpressionText))
                        break;

                    ValidateExpression(
                        mutation.Expression,
                        mutation.ExpressionText,
                        line,
                        context,
                        innerKind,
                        $"'{mutation.Verb.ToString().ToLowerInvariant()} {mutation.TargetField}' value",
                        diagnostics,
                        expressions,
                        stateContext,
                        eventName);

                    // SYNC:CONSTRAINT:C68: literal must be a member of the choice collection's set
                    if (mutation.Verb != PreceptCollectionMutationVerb.Remove
                        && collectionField.ChoiceValues?.Count > 0
                        && mutation.Expression is PreceptLiteralExpression { Value: string literalMember }
                        && !collectionField.ChoiceValues.Contains(literalMember, StringComparer.Ordinal))
                    {
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C68,
                            DiagnosticCatalog.C68.FormatMessage(
                                ("value", literalMember),
                                ("values", string.Join(", ", collectionField.ChoiceValues.Select(v => $"\"{v}\""))),
                                ("name", mutation.TargetField)),
                            line,
                            Column: mutation.Expression.Position?.StartColumn ?? 0,
                            StateContext: stateContext));
                    }
                    break;

                case PreceptCollectionMutationVerb.Dequeue:
                case PreceptCollectionMutationVerb.Pop:
                    if (string.IsNullOrWhiteSpace(mutation.IntoField) || !dataFieldKinds.TryGetValue(mutation.IntoField!, out var intoKind))
                        break;

                    if (IsAssignable(innerKind, intoKind))
                        break;

                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C43,
                        $"'{mutation.Verb.ToString().ToLowerInvariant()} {mutation.TargetField} into {mutation.IntoField}': cannot assign {FormatKinds(innerKind)} to target '{mutation.IntoField}' of type {FormatKinds(intoKind)}.",
                        line,
                        Column: mutation.Expression?.Position?.StartColumn ?? 0,
                        StateContext: stateContext));
                    break;
            }
        }
    }

    private static StaticValueKind MapKind(PreceptScalarType type, bool isNullable)
    {
        var kind = MapScalarTypeToKind(type);
        if (isNullable)
            kind |= StaticValueKind.Null;

        return kind;
    }

    private static StaticValueKind MapScalarTypeToKind(PreceptScalarType type) => type switch
    {
        PreceptScalarType.String => StaticValueKind.String,
        PreceptScalarType.Number => StaticValueKind.Number,
        PreceptScalarType.Boolean => StaticValueKind.Boolean,
        PreceptScalarType.Null => StaticValueKind.Null,
        PreceptScalarType.Integer => StaticValueKind.Integer,
        PreceptScalarType.Decimal => StaticValueKind.Decimal,
        PreceptScalarType.Choice => StaticValueKind.String,  // choice values are strings at runtime
        _ => StaticValueKind.None
    };

    private static StaticValueKind MapLiteralKind(object? value) => value switch
    {
        null => StaticValueKind.Null,
        string => StaticValueKind.String,
        bool => StaticValueKind.Boolean,
        long => StaticValueKind.Integer,
        byte or sbyte or short or ushort or int or uint or ulong or float or double or decimal => StaticValueKind.Number,
        _ => StaticValueKind.None
    };

    private static bool TryGetIdentifierKey(PreceptExpression expression, out string key)
    {
        var stripped = StripParentheses(expression);
        if (stripped is PreceptIdentifierExpression identifier)
        {
            key = identifier.Member is null
                ? identifier.Name
                : $"{identifier.Name}.{identifier.Member}";
            return true;
        }

        key = string.Empty;
        return false;
    }

    private static bool IsNullLiteral(PreceptExpression expression)
        => StripParentheses(expression) is PreceptLiteralExpression { Value: null };

    private static PreceptExpression StripParentheses(PreceptExpression expression)
    {
        while (expression is PreceptParenthesizedExpression parenthesized)
            expression = parenthesized.Inner;

        return expression;
    }

    private static bool IsExactly(StaticValueKind kind, StaticValueKind expected)
        => kind == expected;

    /// <summary>Normalizes OrderedChoice and UnorderedChoice to String for assignability and equality checks.
    /// Choice values are strings at runtime; the distinction only matters for ordinal comparison (>/<).
    /// </summary>
    private static StaticValueKind NormalizeChoiceKind(StaticValueKind k)
    {
        var withoutNull = k & ~StaticValueKind.Null;
        var nullBit = k & StaticValueKind.Null;
        if (withoutNull == StaticValueKind.OrderedChoice || withoutNull == StaticValueKind.UnorderedChoice)
            return StaticValueKind.String | nullBit;
        return k;
    }

    private static bool IsAssignable(StaticValueKind actual, StaticValueKind expected)
    {
        // Normalize choice kinds: choice values are strings at runtime and are assignment-compatible with string.
        actual = NormalizeChoiceKind(actual);
        expected = NormalizeChoiceKind(expected);

        var actualNonNull = actual & ~StaticValueKind.Null;
        var expectedNonNull = expected & ~StaticValueKind.Null;

        if (!HasFlag(expected, StaticValueKind.Null) && HasFlag(actual, StaticValueKind.Null))
            return false;

        // Integer widens to Number and Decimal (explicit widening rules from #29)
        if (actualNonNull == StaticValueKind.Integer &&
            (expectedNonNull == StaticValueKind.Number || expectedNonNull == StaticValueKind.Decimal))
            return true;

        if ((actualNonNull & ~expectedNonNull) != StaticValueKind.None)
            return false;

        if (actual == StaticValueKind.Null)
            return HasFlag(expected, StaticValueKind.Null);

        return true;
    }

    private static bool HasFlag(StaticValueKind kind, StaticValueKind flag)
        => (kind & flag) == flag;

    /// <summary>Returns true when <paramref name="k"/> is a pure numeric kind (Number, Integer, or Decimal, non-nullable, no other flags).</summary>
    private static bool IsNumericKind(StaticValueKind k)
        => IsExactly(k, StaticValueKind.Number) || IsExactly(k, StaticValueKind.Integer) || IsExactly(k, StaticValueKind.Decimal);

    /// <summary>Returns a human-readable label for a <see cref="StaticValueKind"/> (used in function diagnostic messages).</summary>
    private static string KindLabel(StaticValueKind k) => (k & ~StaticValueKind.Null) switch
    {
        StaticValueKind.Integer => "integer",
        StaticValueKind.Decimal => "decimal",
        StaticValueKind.Number => "number",
        StaticValueKind.String => "string",
        StaticValueKind.Boolean => "boolean",
        StaticValueKind.Number | StaticValueKind.Integer | StaticValueKind.Decimal => "numeric",
        StaticValueKind.Number | StaticValueKind.Integer => "number or integer",
        _ => k.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// Resolves the result kind for a numeric binary operation.
    /// Integer × Integer → Integer; Decimal × Decimal|×Integer → Decimal; anything involving Number → Number.
    /// </summary>
    private static StaticValueKind ResolveNumericResultKind(StaticValueKind left, StaticValueKind right)
    {
        if (IsExactly(left, StaticValueKind.Integer) && IsExactly(right, StaticValueKind.Integer))
            return StaticValueKind.Integer;
        if ((IsExactly(left, StaticValueKind.Decimal) || IsExactly(left, StaticValueKind.Integer)) &&
            (IsExactly(right, StaticValueKind.Decimal) || IsExactly(right, StaticValueKind.Integer)))
            return StaticValueKind.Decimal;
        return StaticValueKind.Number;
    }

    /// <summary>Returns true when assigning a Number or Decimal to an Integer target (narrowing, requires explicit conversion).</summary>
    private static bool IsNarrowingToInteger(StaticValueKind actual, StaticValueKind expected)
    {
        var expectedNonNull = expected & ~StaticValueKind.Null;
        var actualNonNull = actual & ~StaticValueKind.Null;
        return expectedNonNull == StaticValueKind.Integer &&
               (actualNonNull == StaticValueKind.Number || actualNonNull == StaticValueKind.Decimal);
    }

    /// <summary>
    /// Returns a numeric-specific author-oriented mismatch message for C39,
    /// or <see langword="null"/> when the mismatch is not between numeric kinds.
    /// </summary>
    private static string? TryBuildNumericMismatchMessage(
        StaticValueKind actual, StaticValueKind expected, string expectedLabel)
    {
        var actualBase   = actual   & ~StaticValueKind.Null;
        var expectedBase = expected & ~StaticValueKind.Null;

        // decimal → number: intentionally unsupported (would silently discard precision)
        if (actualBase == StaticValueKind.Decimal && expectedBase == StaticValueKind.Number)
            return $"{expectedLabel} type mismatch: decimal cannot be assigned to number — " +
                   "this conversion is intentionally unsupported to prevent silent precision loss. " +
                   "Change the field type to decimal, or use floor(), ceil(), truncate(), or round() " +
                   "to drop the fractional part explicitly.";

        // number → decimal: requires explicit authored normalization path
        if (actualBase == StaticValueKind.Number && expectedBase == StaticValueKind.Decimal)
            return $"{expectedLabel} type mismatch: number cannot be implicitly assigned to decimal. " +
                   "Use round(expr, N) to normalize to a specific decimal precision, " +
                   "or change the field type to number.";

        return null;
    }

    /// <summary>
    /// Builds a C60 narrowing-assignment message that advertises only conversion functions
    /// whose return type is <c>integer</c> for the given source kind.
    /// <list type="bullet">
    ///   <item><c>floor()</c>, <c>ceil()</c>, <c>truncate()</c> — return <c>integer</c> for both <c>number</c> and <c>decimal</c> sources.</item>
    ///   <item><c>round(decimal)</c> — returns <c>integer</c>; advertised for <c>decimal</c> sources.</item>
    ///   <item><c>round(number)</c> — returns <c>integer</c>; advertised for <c>number</c> sources.</item>
    /// </list>
    /// </summary>
    private static string BuildC60Message(StaticValueKind actualKind, string fieldName)
    {
        var actualLabel = FormatKinds(actualKind);
        return $"Narrowing assignment: {actualLabel} cannot be implicitly narrowed to integer field '{fieldName}'. Use floor(), ceil(), truncate(), or round() to produce an integer value.";
    }

    /// <summary>
    /// Builds a C79 branch-type-mismatch message, providing numeric widening guidance
    /// when both branches are numeric kinds that do not unify.
    /// </summary>
    private static string BuildC79Message(StaticValueKind thenKind, StaticValueKind elseKind)
    {
        var thenBase = thenKind & ~StaticValueKind.Null;
        var elseBase = elseKind & ~StaticValueKind.Null;

        var bothNumeric = IsNumericKind(thenBase) && IsNumericKind(elseBase);
        if (bothNumeric)
            return DiagnosticCatalog.C79.FormatMessage(
                ("thenType", FormatKinds(thenKind)),
                ("elseType", FormatKinds(elseKind)),
                ("hint", "integer widens to both number and decimal, but number and decimal " +
                         "do not unify with each other. Make both branches the same numeric kind."));

        return DiagnosticCatalog.C79.FormatMessage(
            ("thenType", FormatKinds(thenKind)),
            ("elseType", FormatKinds(elseKind)),
            ("hint", "Make both branches the same scalar type."));
    }
}