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
    int EndColumn = 0,
    string? StateContext = null,
    ProofAssessment? Assessment = null)
{
    public string DiagnosticCode => DiagnosticCatalog.ToDiagnosticCode(Constraint.Id);
}

internal static class PreceptValidationDiagnosticFactory
{
    public static PreceptValidationDiagnostic FromExpression(
        LanguageConstraint constraint,
        string message,
        int line,
        PreceptExpression? expression,
        string? stateContext = null,
        ProofAssessment? assessment = null)
        => new(
            constraint,
            message,
            line,
            Column: expression?.Position?.StartColumn ?? 0,
            EndColumn: expression?.Position?.EndColumn ?? 0,
            StateContext: stateContext,
            Assessment: assessment);

    public static PreceptValidationDiagnostic FromColumns(
        LanguageConstraint constraint,
        string message,
        int line,
        int startColumn,
        int endColumn,
        string? stateContext = null,
        ProofAssessment? assessment = null)
        => new(
            constraint,
            message,
            line,
            Column: startColumn,
            EndColumn: endColumn,
            StateContext: stateContext,
            Assessment: assessment);
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
    IReadOnlyList<string>? ComputedFieldOrder = null,
    GlobalProofContext? ProofContext = null)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
}

internal sealed record ValidationResult(
    IReadOnlyList<PreceptValidationDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext,
    PreceptDefinition? ValidatedModel = null,
    GlobalProofContext? ProofContext = null)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
}

// Partial-class file for PreceptTypeChecker — main orchestration.
// Hosts the front-matter types (StaticValueKind, diagnostics, TypeCheckResult, etc.)
// and the primary orchestration entry points (Check, ValidateTransitionRows,
// ValidateStateActions, ValidateRules, ValidateCollectionMutations, and related helpers).
// Extracted logic lives in: Helpers, FieldConstraints, Narrowing, ProofChecks, TypeInference.
internal static partial class PreceptTypeChecker
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

        return new TypeCheckResult(diagnostics, new PreceptTypeContext(expressions, scopes), computedFieldOrder, dataFieldKinds);
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
                        var guardSourceLine = row.GuardSourceLine > 0 ? row.GuardSourceLine : row.SourceLine;

                        scopes.Add(new PreceptTypeScopeInfo(
                            guardSourceLine,
                            "when",
                            new Dictionary<string, StaticValueKind>(branchContext.Symbols, StringComparer.Ordinal),
                            item.State,
                            eventName));
                        ValidateExpression(
                            row.WhenGuard,
                            row.WhenText!,
                            guardSourceLine,
                            branchContext,
                            StaticValueKind.Boolean,
                            "when predicate",
                            diagnostics,
                            expressions,
                            stateContext: item.State,
                            isBooleanRulePosition: true);

                        // SYNC:CONSTRAINT:C97/C98: dead/tautological guard detection
                        AssessGuard(row.WhenGuard, row.WhenText!, guardSourceLine,
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
                                EndColumn: assignment.Expression.Position?.EndColumn ?? 0,
                                StateContext: item.State));
                        }

                        // SYNC:CONSTRAINT:C94: assignment provably outside field constraint range
                        if (dataFieldKinds.FieldIntervals.TryGetValue(assignment.Key, out var constraintIval94))
                        {
                            var rhsProof = setContext.IntervalOf(assignment.Expression);
                            var rhsIval = rhsProof.Interval;
                            if (!rhsIval.IsUnknown && NumericInterval.AreDisjoint(rhsIval, constraintIval94))
                            {
                                var assessment = new ProofAssessment(
                                    ProofRequirement.AssignmentConstraint, ProofOutcome.Contradiction,
                                    assignment.Key, rhsIval, rhsProof.Attribution,
                                    ConstraintInterval: constraintIval94);
                                diagnostics.Add(new PreceptValidationDiagnostic(
                                    DiagnosticCatalog.C94,
                                    ProofDiagnosticRenderer.Render(assessment),
                                    assignment.SourceLine > 0 ? assignment.SourceLine : row.SourceLine,
                                    Column: assignment.Expression.Position?.StartColumn ?? 0,
                                    EndColumn: assignment.Expression.Position?.EndColumn ?? 0,
                                    StateContext: item.State,
                                    Assessment: assessment));
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
                        EndColumn: assignment.Expression.Position?.EndColumn ?? 0,
                        StateContext: action.State));
                }

                // SYNC:CONSTRAINT:C94: assignment provably outside field constraint range
                if (dataFieldKinds.FieldIntervals.TryGetValue(assignment.Key, out var constraintIval94))
                {
                    var rhsProof = assignmentContext.IntervalOf(assignment.Expression);
                    var rhsIval = rhsProof.Interval;
                    if (!rhsIval.IsUnknown && NumericInterval.AreDisjoint(rhsIval, constraintIval94))
                    {
                        var assessment = new ProofAssessment(
                            ProofRequirement.AssignmentConstraint, ProofOutcome.Contradiction,
                            assignment.Key, rhsIval, rhsProof.Attribution,
                            ConstraintInterval: constraintIval94);
                        diagnostics.Add(new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C94,
                            ProofDiagnosticRenderer.Render(assessment),
                            assignment.SourceLine > 0 ? assignment.SourceLine : action.SourceLine,
                            Column: assignment.Expression.Position?.StartColumn ?? 0,
                            EndColumn: assignment.Expression.Position?.EndColumn ?? 0,
                            StateContext: action.State,
                            Assessment: assessment));
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
                            rule.SourceLine,
                            Assessment: assessment));
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
                            rule.SourceLine,
                            Assessment: assessment));
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
                        Column: id.Position?.StartColumn ?? 0,
                        EndColumn: id.Position?.EndColumn ?? 0));
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
                            EndColumn: mutation.Expression.Position?.EndColumn ?? 0,
                            StateContext: stateContext));
                    }
                    break;

                case PreceptCollectionMutationVerb.Dequeue:
                case PreceptCollectionMutationVerb.Pop:
                    if (string.IsNullOrWhiteSpace(mutation.IntoField) || !dataFieldKinds.TryGetValue(mutation.IntoField!, out var intoKind))
                        break;

                    if (IsAssignable(innerKind, intoKind))
                        break;

                    diagnostics.Add(PreceptValidationDiagnosticFactory.FromColumns(
                        DiagnosticCatalog.C43,
                        $"'{mutation.Verb.ToString().ToLowerInvariant()} {mutation.TargetField} into {mutation.IntoField}': cannot assign {FormatKinds(innerKind)} to target '{mutation.IntoField}' of type {FormatKinds(intoKind)}.",
                        line,
                        mutation.IntoFieldStartColumn,
                        mutation.IntoFieldEndColumn,
                        stateContext));
                    break;
            }
        }
    }

}
